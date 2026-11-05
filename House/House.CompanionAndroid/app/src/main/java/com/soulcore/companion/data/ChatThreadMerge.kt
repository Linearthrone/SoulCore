package com.soulcore.companion.data

import com.soulcore.companion.net.HydratedMessage
import java.time.Instant

/**
 * Folding Host-durable transcript rows into the on-device thread.
 *
 * Kept free of Android and coroutines so the merge policy — which is where the
 * interesting cases live — can be unit tested directly. [ChatStore] owns the
 * plumbing; this owns the decisions.
 */
internal object ChatThreadMerge {

    data class Result(val messages: List<ChatMessage>, val added: Int)

    /**
     * Merge [incoming] Host rows into [current], skipping anything already held.
     *
     * Dedupe is on `frameId`, which both sides agree on: the Host files a reply under
     * the `chat.done` frame id and the operator's line under `<id>:user`, and the client
     * records both. Rows older than everything held locally are prepended as history and
     * newer rows append — covering the three real cases: fresh install, a gap while the
     * phone was offline, and backfilling behind a thread already partly present.
     *
     * Only `user` / `assistant` rows are taken; the transcript stores conversation, and
     * connection chatter is never part of it.
     */
    fun merge(
        current: List<ChatMessage>,
        incoming: List<HydratedMessage>,
        maxMessages: Int = ChatStore.MAX_MESSAGES
    ): Result {
        if (incoming.isEmpty()) return Result(current, 0)

        val knownFrameIds = current.mapNotNullTo(HashSet()) { it.frameId }
        val oldestLocal = current
            .filter { it.role == MessageRole.USER || it.role == MessageRole.ASSISTANT }
            .minOfOrNull { it.timestampMs }

        val older = ArrayList<ChatMessage>()
        val newer = ArrayList<ChatMessage>()

        for (row in incoming) {
            val role = roleOf(row.role) ?: continue
            if (row.frameId != null && !knownFrameIds.add(row.frameId)) continue
            if (row.content.isBlank() && row.mediaId == null) continue

            val ts = parseOccurredAt(row.occurredAt)
            val message = ChatMessage(
                role = role,
                content = row.content,
                frameId = row.frameId,
                timestampMs = ts,
                mediaId = row.mediaId
            )
            if (oldestLocal != null && ts < oldestLocal) older.add(message) else newer.add(message)
        }

        if (older.isEmpty() && newer.isEmpty()) return Result(current, 0)
        return Result(
            messages = (older + current + newer).takeLast(maxMessages),
            added = older.size + newer.size
        )
    }

    private fun roleOf(raw: String): MessageRole? = when (raw.lowercase()) {
        "user" -> MessageRole.USER
        "assistant" -> MessageRole.ASSISTANT
        else -> null
    }

    /** Falls back to "now" rather than dropping a turn over an unparseable timestamp. */
    fun parseOccurredAt(value: String): Long =
        runCatching { Instant.parse(value).toEpochMilli() }
            .getOrElse { System.currentTimeMillis() }
}
