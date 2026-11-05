package com.soulcore.companion.net

import okhttp3.OkHttpClient
import okhttp3.Request
import org.json.JSONObject
import java.util.concurrent.TimeUnit

/** One durable turn as the Host stores it. */
data class HydratedMessage(
    /** Server row id — the cursor value for this turn. */
    val id: Long,
    val role: String,
    val content: String,
    /** ISO-8601 UTC, e.g. `2026-09-27T17:23:12.682Z`. */
    val occurredAt: String,
    val channel: String?,
    val frameId: String?,
    val mediaId: String?
)

/**
 * One page of the durable transcript.
 *
 * @param cursor highest row id in this page, to pass back as `after`.
 * @param latestCursor highest row id the Host holds for the conversation.
 * @param hasMore true when rows remain past this page.
 */
data class HydratePage(
    val conversationId: String,
    val messages: List<HydratedMessage>,
    val cursor: Long,
    val latestCursor: Long,
    val hasMore: Boolean
)

/**
 * Reads the Host-durable transcript (`GET /api/companion/v1/messages`).
 *
 * PROP-3 Wave 1: the phone's on-device thread is a cache, and the Host is the
 * archive. This is how a fresh install backfills desk and SMS history instead of
 * starting empty.
 */
object ChatHydrateClient {
    private val http = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()

    /**
     * Fetch turns after [after], oldest first.
     *
     * @param recent when true and [after] is 0, return the newest [limit] turns rather
     *   than the start of the conversation — the right call for a cold client facing a
     *   long thread.
     */
    fun fetch(
        httpBase: String,
        token: String,
        conversationId: String? = null,
        after: Long = 0,
        limit: Int = 100,
        recent: Boolean = false
    ): Result<HydratePage> = runCatching {
        val url = StringBuilder("${httpBase.trimEnd('/')}/api/companion/v1/messages")
            .append("?after=").append(if (after < 0) 0 else after)
            .append("&limit=").append(limit)
        if (recent) url.append("&recent=true")
        if (!conversationId.isNullOrBlank()) {
            url.append("&conversationId=").append(conversationId.trim())
        }

        val builder = Request.Builder().url(url.toString()).get()
        CompanionAuthHeaders.applyBearer(builder, token)
        http.newCall(builder.build()).execute().use { resp ->
            val body = resp.body?.string().orEmpty()
            if (!resp.isSuccessful) error("HTTP ${resp.code}: ${body.take(160)}")
            parsePage(body)
        }
    }

    /**
     * Parse a hydrate response body. Split out from the request so the wire contract with
     * `CompanionApiEndpoints` can be asserted against a captured Host response without a
     * live socket — a field rename on either side should fail a test, not a phone.
     */
    fun parsePage(body: String): HydratePage {
        val root = JSONObject(body)
        val arr = root.optJSONArray("messages")
        val messages = buildList {
            for (i in 0 until (arr?.length() ?: 0)) {
                val o = arr!!.optJSONObject(i) ?: continue
                add(
                    HydratedMessage(
                        id = o.optLong("id"),
                        role = o.optString("role"),
                        content = o.optString("content"),
                        occurredAt = o.optString("occurredAt"),
                        channel = o.optString("channel").ifBlank { null },
                        frameId = o.optString("frameId").ifBlank { null },
                        mediaId = o.optString("mediaId").ifBlank { null }
                    )
                )
            }
        }
        return HydratePage(
            conversationId = root.optString("conversationId"),
            messages = messages,
            cursor = root.optLong("cursor"),
            latestCursor = root.optLong("latestCursor"),
            hasMore = root.optBoolean("hasMore", false)
        )
    }
}
