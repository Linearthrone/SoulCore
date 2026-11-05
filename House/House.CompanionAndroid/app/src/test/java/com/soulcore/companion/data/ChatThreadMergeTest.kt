package com.soulcore.companion.data

import com.soulcore.companion.net.HydratedMessage
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * PROP-3 Wave 1: hydrating the phone thread from the Host-durable transcript.
 *
 * The cases that matter are a fresh install (local empty), a gap while the phone was
 * offline, backfilling behind a thread already partly present, and — most importantly —
 * not double-rendering a turn the client already saw live over `chat.done`.
 */
class ChatThreadMergeTest {

    @Test
    fun `fresh install takes the whole page in order`() {
        val result = ChatThreadMerge.merge(
            current = emptyList(),
            incoming = listOf(
                host(1, "user", "morning", at = "2026-09-27T10:00:00.000Z"),
                host(2, "assistant", "morning to you", at = "2026-09-27T10:00:01.000Z")
            )
        )

        assertEquals(2, result.added)
        assertEquals(listOf("morning", "morning to you"), result.messages.map { it.content })
        assertEquals(
            listOf(MessageRole.USER, MessageRole.ASSISTANT),
            result.messages.map { it.role }
        )
    }

    @Test
    fun `a turn already seen live is not duplicated`() {
        // The client rendered the reply from chat.done, so it holds frameId "f1".
        val local = listOf(
            ChatMessage(role = MessageRole.USER, content = "ping", frameId = "f1:user", timestampMs = 5_000),
            ChatMessage(role = MessageRole.ASSISTANT, content = "pong", frameId = "f1", timestampMs = 5_001)
        )

        val result = ChatThreadMerge.merge(
            current = local,
            incoming = listOf(
                host(1, "user", "ping", frameId = "f1:user", at = "2026-09-27T10:00:00.000Z"),
                host(2, "assistant", "pong", frameId = "f1", at = "2026-09-27T10:00:01.000Z")
            )
        )

        assertEquals(0, result.added)
        assertEquals(2, result.messages.size)
    }

    @Test
    fun `only the unseen half of a page is added`() {
        val local = listOf(
            ChatMessage(role = MessageRole.ASSISTANT, content = "pong", frameId = "f1", timestampMs = 5_000)
        )

        val result = ChatThreadMerge.merge(
            current = local,
            incoming = listOf(
                host(1, "assistant", "pong", frameId = "f1", at = "2026-09-27T10:00:00.000Z"),
                host(2, "user", "new from desk", frameId = "f2:user", at = "2026-09-27T10:05:00.000Z")
            )
        )

        assertEquals(1, result.added)
        assertEquals(listOf("pong", "new from desk"), result.messages.map { it.content })
    }

    @Test
    fun `desk talk that arrived while offline appends after local rows`() {
        val local = listOf(
            ChatMessage(role = MessageRole.USER, content = "older local", timestampMs = 1_000)
        )

        val result = ChatThreadMerge.merge(
            current = local,
            incoming = listOf(host(9, "assistant", "said while you were away", at = "2026-09-27T23:00:00.000Z"))
        )

        assertEquals(1, result.added)
        assertEquals(listOf("older local", "said while you were away"), result.messages.map { it.content })
    }

    @Test
    fun `history older than the local thread is prepended`() {
        // Local tail is "recent"; the Host hands back genuinely older history.
        val local = listOf(
            ChatMessage(role = MessageRole.USER, content = "recent", timestampMs = 9_000_000_000_000)
        )

        val result = ChatThreadMerge.merge(
            current = local,
            incoming = listOf(host(1, "user", "ancient", at = "2020-01-01T00:00:00.000Z"))
        )

        assertEquals(1, result.added)
        assertEquals(listOf("ancient", "recent"), result.messages.map { it.content })
    }

    @Test
    fun `rows without a frame id are still taken`() {
        // SMS and proactive rows can reach the transcript without a client-known frame id.
        val result = ChatThreadMerge.merge(
            current = emptyList(),
            incoming = listOf(
                host(1, "user", "texted in", frameId = null, at = "2026-09-27T10:00:00.000Z"),
                host(2, "assistant", "replied", frameId = null, at = "2026-09-27T10:00:01.000Z")
            )
        )

        assertEquals(2, result.added)
    }

    @Test
    fun `non conversation roles are ignored`() {
        val result = ChatThreadMerge.merge(
            current = emptyList(),
            incoming = listOf(
                host(1, "system", "WS connected", at = "2026-09-27T10:00:00.000Z"),
                host(2, "tool", "ran something", at = "2026-09-27T10:00:01.000Z"),
                host(3, "user", "real turn", at = "2026-09-27T10:00:02.000Z")
            )
        )

        assertEquals(1, result.added)
        assertEquals(listOf("real turn"), result.messages.map { it.content })
    }

    @Test
    fun `blank rows are skipped unless they carry media`() {
        val result = ChatThreadMerge.merge(
            current = emptyList(),
            incoming = listOf(
                host(1, "assistant", "   ", at = "2026-09-27T10:00:00.000Z"),
                host(2, "assistant", "", mediaId = "media-7", at = "2026-09-27T10:00:01.000Z")
            )
        )

        assertEquals(1, result.added)
        assertEquals("media-7", result.messages.single().mediaId)
    }

    @Test
    fun `merge is idempotent across repeated hydrates`() {
        val page = listOf(
            host(1, "user", "once", frameId = "f1:user", at = "2026-09-27T10:00:00.000Z"),
            host(2, "assistant", "only once", frameId = "f1", at = "2026-09-27T10:00:01.000Z")
        )

        val first = ChatThreadMerge.merge(emptyList(), page)
        val second = ChatThreadMerge.merge(first.messages, page)
        val third = ChatThreadMerge.merge(second.messages, page)

        assertEquals(2, first.added)
        assertEquals(0, second.added)
        assertEquals(0, third.added)
        assertEquals(2, third.messages.size)
    }

    @Test
    fun `empty page changes nothing`() {
        val local = listOf(ChatMessage(role = MessageRole.USER, content = "keep me"))
        val result = ChatThreadMerge.merge(local, emptyList())

        assertEquals(0, result.added)
        assertEquals(local, result.messages)
    }

    @Test
    fun `thread is capped so a long archive cannot grow unbounded`() {
        val incoming = (1..40).map { host(it.toLong(), "user", "m$it", frameId = "f$it") }

        val result = ChatThreadMerge.merge(emptyList(), incoming, maxMessages = 10)

        assertEquals(40, result.added)
        assertEquals(10, result.messages.size)
        // The cap keeps the newest rows, matching takeLast elsewhere in the store.
        assertEquals("m40", result.messages.last().content)
    }

    @Test
    fun `system rows in the local thread do not break the merge`() {
        val local = listOf(
            ChatMessage(role = MessageRole.SYSTEM, content = "WS connected", timestampMs = 1),
            ChatMessage(role = MessageRole.USER, content = "hi", timestampMs = 5_000)
        )

        val result = ChatThreadMerge.merge(
            current = local,
            incoming = listOf(host(1, "assistant", "hello", at = "2026-09-27T23:00:00.000Z"))
        )

        assertEquals(1, result.added)
        // The SYSTEM row is not a conversation turn, so it must not be treated as the
        // oldest local turn when deciding prepend vs append.
        assertEquals(listOf("WS connected", "hi", "hello"), result.messages.map { it.content })
    }

    @Test
    fun `unparseable timestamp falls back to now instead of dropping the turn`() {
        val before = System.currentTimeMillis()
        val parsed = ChatThreadMerge.parseOccurredAt("not-a-timestamp")

        assertTrue(parsed >= before)

        val result = ChatThreadMerge.merge(
            current = emptyList(),
            incoming = listOf(host(1, "user", "still counted", at = "garbage"))
        )
        assertEquals(1, result.added)
    }

    private fun host(
        id: Long,
        role: String,
        content: String,
        at: String = "2026-09-27T10:00:00.000Z",
        frameId: String? = null,
        mediaId: String? = null
    ) = HydratedMessage(
        id = id,
        role = role,
        content = content,
        occurredAt = at,
        channel = "desk",
        frameId = frameId,
        mediaId = mediaId
    )
}
