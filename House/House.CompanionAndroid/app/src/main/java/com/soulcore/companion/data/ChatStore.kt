package com.soulcore.companion.data

import android.app.Application
import android.util.Log
import com.soulcore.companion.net.ChatHydrateClient
import com.soulcore.companion.net.CompanionConnection
import com.soulcore.companion.net.CompanionMediaClient
import com.soulcore.companion.net.HydratedMessage
import com.soulcore.companion.net.SoulCoreFrame
import com.soulcore.companion.net.WsConnectionState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.util.concurrent.atomic.AtomicReference

/**
 * Process-scoped transcript for the operator <-> companion thread.
 *
 * PROP-3 Wave 1, "stop Compose-list-as-truth". [com.soulcore.companion.ui.ChatScreen]
 * used to hold the thread in `remember { mutableStateListOf() }`, which has two
 * consequences: navigating to Settings / Call / Gallery destroys the list, and
 * any reply arriving while the screen is gone is dropped even though the
 * foreground service kept the socket open. Frames are collected here for the
 * process lifetime instead, and USER/ASSISTANT turns are mirrored to disk, so
 * neither leaving the screen nor process death wipes the thread.
 *
 * Disk copy is a *cache* of the tail, not an archive — the Host owns history.
 */
object ChatStore {
    private const val TAG = "ChatStore"
    private const val FILE_NAME = "chat-thread.json"

    /** Tail kept on device. The phone is a cache; the Host is the archive. */
    const val MAX_MESSAGES = 500

    /** Suffix the Host files the operator's line under, relative to the `chat.send` id. */
    const val USER_FRAME_SUFFIX = ":user"

    private const val HYDRATE_PAGE_SIZE = 100

    /** Bounds a single backfill so a very long archive cannot block startup forever. */
    private const val MAX_HYDRATE_PAGES = 10

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val lock = Any()

    /** Highest Host row id already merged. Persisted so backfill stays incremental. */
    private var hydrateCursor = 0L

    private val _messages = MutableStateFlow<List<ChatMessage>>(emptyList())
    val messages: StateFlow<List<ChatMessage>> = _messages.asStateFlow()

    /** Id of the assistant bubble currently being streamed into, if any. */
    private val streamingAssistantId = AtomicReference<String?>(null)

    private var app: Application? = null
    private var installed = false

    /** Load the cached tail and start collecting hub frames for the process lifetime. */
    fun install(application: Application) {
        if (installed) return
        installed = true
        app = application

        scope.launch {
            restore(application)
            CompanionConnection.frames.collect { frame -> applyFrame(frame) }
        }
        scope.launch {
            // Backfill whenever the socket comes up: on a fresh install that is the
            // whole thread, and after time offline it is whatever the desk said while
            // the phone was away.
            CompanionConnection.state.collect { (state, _) ->
                if (state == WsConnectionState.Connected) hydrate()
            }
        }
        Log.i(TAG, "Transcript store installed on CompanionConnection.frames")
    }

    // ---------- local appends ----------

    /**
     * Append the operator's own turn (already formatted for display).
     *
     * [sendFrameId] is the id returned by [com.soulcore.companion.net.SoulCoreWsClient.sendChat].
     * The Host files the operator's line under `<frameId>:user`, so recording it here
     * lets [hydrate] recognise this exact row later instead of appending a copy.
     */
    fun addUser(content: String, sendFrameId: String? = null) {
        streamingAssistantId.set(null)
        append(
            ChatMessage(
                role = MessageRole.USER,
                content = content,
                frameId = sendFrameId?.let { "$it$USER_FRAME_SUFFIX" }
            )
        )
    }

    /**
     * Append a diagnostic line. System/error rows are session-only: connection
     * chatter does not belong in the durable thread.
     *
     * Repeats are dropped. Every return to Chat re-emits the current connection
     * detail, which used to be harmless only because leaving the screen threw
     * the list away; now that the thread survives, it would stack up identical
     * "WS connected" bubbles.
     */
    fun addSystem(content: String) {
        synchronized(lock) {
            val last = _messages.value.lastOrNull { it.role == MessageRole.SYSTEM }
            if (last?.content == content) return
        }
        append(ChatMessage(role = MessageRole.SYSTEM, content = content))
    }

    fun addError(content: String) {
        append(ChatMessage(role = MessageRole.ERROR, content = content))
    }

    /** Drop the on-device cache and the in-memory thread. */
    fun clear() {
        synchronized(lock) {
            _messages.value = emptyList()
            streamingAssistantId.set(null)
            // Forget the cursor too, so the next connect re-backfills from the Host
            // instead of leaving the operator with a permanently empty thread.
            hydrateCursor = 0L
        }
        scope.launch {
            runCatching { fileFor(app)?.delete() }
                .onFailure { Log.w(TAG, "Could not delete cached thread", it) }
        }
    }

    // ---------- hydrate from the Host ----------

    /**
     * Backfill from the Host-durable transcript.
     *
     * The device cache only ever held what this phone personally witnessed, so a fresh
     * install started empty and time spent offline left holes. The Host owns the archive;
     * this pulls the part we are missing.
     *
     * A cold client (cursor 0) asks for the newest page rather than the start of the
     * conversation, so a long history does not have to be walked to show something useful.
     * After that it is strictly incremental via `after=<cursor>`.
     */
    fun hydrate() {
        val application = app ?: return
        scope.launch {
            val cfg = CompanionPrefs.load(application)
            var cursor = synchronized(lock) { hydrateCursor }
            val cold = cursor <= 0L
            var pages = 0
            var added = 0

            while (pages++ < MAX_HYDRATE_PAGES) {
                val page = ChatHydrateClient.fetch(
                    httpBase = cfg.resolvedHttpBase(),
                    token = cfg.token,
                    after = cursor,
                    limit = HYDRATE_PAGE_SIZE,
                    recent = cold && pages == 1
                ).getOrElse { err ->
                    // Offline or Host down is normal; the cache still renders.
                    Log.w(TAG, "Hydrate failed (cursor=$cursor)", err)
                    return@launch
                }

                added += mergeHydrated(page.messages)
                if (page.cursor > cursor) cursor = page.cursor
                if (!page.hasMore || page.messages.isEmpty()) break
            }

            synchronized(lock) { hydrateCursor = cursor }
            if (added > 0) {
                persist()
                Log.i(TAG, "Hydrated $added message(s) from Host; cursor=$cursor")
            }
        }
    }

    /** Apply [ChatThreadMerge] under the lock. Returns how many rows were added. */
    private fun mergeHydrated(incoming: List<HydratedMessage>): Int {
        if (incoming.isEmpty()) return 0
        synchronized(lock) {
            val result = ChatThreadMerge.merge(_messages.value, incoming, MAX_MESSAGES)
            if (result.added > 0) _messages.value = result.messages
            return result.added
        }
    }

    // ---------- frame handling ----------

    /**
     * Fold a hub frame into the thread. Ported from ChatScreen so it keeps
     * running while no Compose screen is alive.
     */
    fun applyFrame(frame: SoulCoreFrame) {
        when (frame.type) {
            SoulCoreFrame.CHAT_DELTA -> appendOrUpdateAssistant(frame, finalize = false)
            SoulCoreFrame.CHAT_DONE -> appendOrUpdateAssistant(frame, finalize = true)
            SoulCoreFrame.ERROR -> {
                streamingAssistantId.set(null)
                val code = frame.payloadString("code")
                val msg = frame.payloadString("message") ?: frame.payload?.toString().orEmpty()
                addError("error${code?.let { " [$it]" } ?: ""}: $msg")
            }
            SoulCoreFrame.PRESENCE_STATUS,
            SoulCoreFrame.EMOTION_SNAPSHOT,
            SoulCoreFrame.PONG,
            "loop.want",
            "loop.tick.ok" -> Unit
            else -> addSystem("frame ${frame.type} id=${frame.id}")
        }
    }

    private fun appendOrUpdateAssistant(frame: SoulCoreFrame, finalize: Boolean) {
        val text = frame.payloadText()
        val mediaId = frame.payloadString("mediaId")
        val hasMedia = frame.payload?.optBoolean("hasMedia", false) == true || !mediaId.isNullOrBlank()
        val proactive = frame.payload?.optBoolean("proactive", false) == true

        if (text.isNullOrEmpty() && finalize && !hasMedia) {
            streamingAssistantId.set(null)
            return
        }

        val content = text.orEmpty()
        var mediaTarget: String? = null

        synchronized(lock) {
            val current = _messages.value
            val streamId = streamingAssistantId.get()

            if (streamId != null) {
                val idx = current.indexOfFirst { it.id == streamId }
                if (idx >= 0) {
                    val existing = current[idx]
                    if (frame.id.isBlank() ||
                        existing.frameId == frame.id ||
                        existing.frameId.isNullOrBlank()
                    ) {
                        val updated = current.toMutableList()
                        updated[idx] = existing.copy(
                            content = content.ifEmpty { existing.content },
                            frameId = frame.id.ifBlank { existing.frameId },
                            mediaId = mediaId ?: existing.mediaId,
                            proactive = proactive || existing.proactive
                        )
                        _messages.value = updated
                        if (finalize) {
                            streamingAssistantId.set(null)
                            if (hasMedia && !mediaId.isNullOrBlank()) {
                                mediaTarget = updated[idx].id
                            }
                        }
                        return@synchronized
                    }
                }
            }

            val bubble = ChatMessage(
                role = MessageRole.ASSISTANT,
                content = content.ifEmpty { if (hasMedia) "(image)" else "" },
                frameId = frame.id.ifBlank { null },
                mediaId = mediaId,
                proactive = proactive
            )
            _messages.value = (current + bubble).takeLast(MAX_MESSAGES)
            streamingAssistantId.set(if (finalize) null else bubble.id)
            if (finalize && hasMedia && !mediaId.isNullOrBlank()) {
                mediaTarget = bubble.id
            }
        }

        if (finalize) schedulePersist()

        val target = mediaTarget
        val media = mediaId
        if (target != null && media != null) {
            fetchMedia(target, media)
        }
    }

    /** Download an assistant image into the gallery and attach it to its bubble. */
    private fun fetchMedia(messageId: String, mediaId: String) {
        val application = app ?: return
        scope.launch {
            val cfg = CompanionPrefs.load(application)
            CompanionMediaClient
                .downloadMedia(cfg.resolvedHttpBase(), cfg.token, mediaId)
                .onSuccess { bytes ->
                    val item = GalleryStore.saveBytes(application, bytes, mediaId = mediaId)
                    attachLocalImage(messageId, item.localPath)
                }
                .onFailure { Log.w(TAG, "Media $mediaId download failed", it) }
        }
    }

    private fun attachLocalImage(messageId: String, path: String) {
        synchronized(lock) {
            val current = _messages.value
            val idx = current.indexOfFirst { it.id == messageId }
            if (idx < 0) return
            val updated = current.toMutableList()
            updated[idx] = updated[idx].copy(localImagePath = path)
            _messages.value = updated
        }
        schedulePersist()
    }

    private fun append(message: ChatMessage) {
        synchronized(lock) {
            _messages.value = (_messages.value + message).takeLast(MAX_MESSAGES)
        }
        if (message.role == MessageRole.USER) schedulePersist()
    }

    // ---------- disk cache ----------

    private fun schedulePersist() {
        scope.launch { persist() }
    }

    /**
     * Only USER/ASSISTANT turns are written. Connection chatter and error rows
     * are diagnostics for the current session, not part of the conversation.
     */
    private fun persist() {
        val target = fileFor(app) ?: return
        val snapshot = _messages.value.filter {
            it.role == MessageRole.USER || it.role == MessageRole.ASSISTANT
        }

        try {
            val array = JSONArray()
            for (m in snapshot) {
                array.put(
                    JSONObject().apply {
                        put("id", m.id)
                        put("role", m.role.name)
                        put("content", m.content)
                        put("timestampMs", m.timestampMs)
                        m.frameId?.let { put("frameId", it) }
                        m.mediaId?.let { put("mediaId", it) }
                        m.localImagePath?.let { put("localImagePath", it) }
                        if (m.proactive) put("proactive", true)
                    }
                )
            }
            // The cursor rides with the cache so backfill stays incremental across restarts.
            val root = JSONObject()
                .put("cursor", synchronized(lock) { hydrateCursor })
                .put("messages", array)
                .toString()

            // Write-then-rename so a kill mid-write cannot truncate the thread.
            val tmp = File(target.parentFile, "$FILE_NAME.tmp")
            tmp.writeText(root)
            if (!tmp.renameTo(target)) {
                target.writeText(root)
                tmp.delete()
            }
        } catch (e: Exception) {
            Log.w(TAG, "Could not persist thread", e)
        }
    }

    private fun restore(application: Application) {
        val source = fileFor(application) ?: return
        if (!source.isFile) return

        try {
            val text = source.readText()
            // Older builds wrote a bare array with no cursor; still read those.
            val array = if (text.trimStart().startsWith("[")) {
                JSONArray(text)
            } else {
                val root = JSONObject(text)
                synchronized(lock) { hydrateCursor = root.optLong("cursor", 0L) }
                root.optJSONArray("messages") ?: JSONArray()
            }
            val loaded = ArrayList<ChatMessage>(array.length())
            for (i in 0 until array.length()) {
                val o = array.optJSONObject(i) ?: continue
                val role = runCatching { MessageRole.valueOf(o.optString("role")) }.getOrNull()
                    ?: continue
                loaded.add(
                    ChatMessage(
                        id = o.optString("id").ifBlank { java.util.UUID.randomUUID().toString() },
                        role = role,
                        content = o.optString("content"),
                        frameId = o.optString("frameId").ifBlank { null },
                        timestampMs = o.optLong("timestampMs", System.currentTimeMillis()),
                        mediaId = o.optString("mediaId").ifBlank { null },
                        localImagePath = o.optString("localImagePath").ifBlank { null },
                        proactive = o.optBoolean("proactive", false)
                    )
                )
            }
            synchronized(lock) {
                // Anything already streamed in during startup stays at the tail.
                _messages.value = (loaded.takeLast(MAX_MESSAGES) + _messages.value)
                    .takeLast(MAX_MESSAGES)
            }
            Log.i(TAG, "Restored ${loaded.size} cached messages")
        } catch (e: Exception) {
            Log.w(TAG, "Could not restore cached thread", e)
        }
    }

    private fun fileFor(application: Application?): File? =
        application?.let { File(it.filesDir, FILE_NAME) }
}
