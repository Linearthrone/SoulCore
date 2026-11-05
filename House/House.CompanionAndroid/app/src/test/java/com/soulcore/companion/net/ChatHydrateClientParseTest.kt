package com.soulcore.companion.net

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pins the hydrate wire contract with `SoulCore.Host.Companion.CompanionApiEndpoints`.
 *
 * [REAL_HOST_PAGE] is a verbatim capture from a running Host
 * (`GET /api/companion/v1/messages?after=0&limit=2`), so renaming a field on either side
 * fails here rather than silently producing an empty thread on the phone.
 */
class ChatHydrateClientParseTest {

    @Test
    fun `parses a real Host page`() {
        val page = ChatHydrateClient.parsePage(REAL_HOST_PAGE)

        assertEquals("presence-local", page.conversationId)
        assertEquals(2, page.cursor)
        assertEquals(8, page.latestCursor)
        assertTrue("hasMore must survive the round trip", page.hasMore)
        assertEquals(2, page.messages.size)

        val user = page.messages[0]
        assertEquals(1L, user.id)
        assertEquals("user", user.role)
        assertEquals("Companion, this is the desk talking.", user.content)
        assertEquals("2026-09-27T17:23:12.682Z", user.occurredAt)
        assertEquals("desk", user.channel)
        assertEquals("demo-1790529792497-0:user", user.frameId)
        assertNull("JSON null mediaId must become null, not \"null\"", user.mediaId)

        val assistant = page.messages[1]
        assertEquals("assistant", assistant.role)
        // The assistant row carries the bare chat.done frame id; the user row adds ":user".
        assertEquals("demo-1790529792497-0", assistant.frameId)
        assertEquals(assistant.frameId + ":user", user.frameId)
    }

    @Test
    fun `parses an empty conversation`() {
        val page = ChatHydrateClient.parsePage(
            """{"conversationId":"presence-local","messages":[],"cursor":0,"latestCursor":0,"hasMore":false}"""
        )

        assertTrue(page.messages.isEmpty())
        assertEquals(0, page.cursor)
        assertFalse(page.hasMore)
    }

    @Test
    fun `missing optional fields do not throw`() {
        val page = ChatHydrateClient.parsePage(
            """{"messages":[{"id":5,"role":"user","content":"bare","occurredAt":"2026-09-27T10:00:00.000Z"}]}"""
        )

        val only = page.messages.single()
        assertEquals(5L, only.id)
        assertNull(only.channel)
        assertNull(only.frameId)
        assertNull(only.mediaId)
        assertFalse(page.hasMore)
    }

    private companion object {
        /** Verbatim from a live Host — do not hand-edit. */
        const val REAL_HOST_PAGE = """
{
    "conversationId": "presence-local",
    "messages": [
        {
            "id": 1,
            "role": "user",
            "content": "Companion, this is the desk talking.",
            "occurredAt": "2026-09-27T17:23:12.682Z",
            "channel": "desk",
            "frameId": "demo-1790529792497-0:user",
            "mediaId": null
        },
        {
            "id": 2,
            "role": "assistant",
            "content": "[stub] SoulCore received: Companion, this is the desk talking.",
            "occurredAt": "2026-09-27T17:23:12.689Z",
            "channel": "desk",
            "frameId": "demo-1790529792497-0",
            "mediaId": null
        }
    ],
    "cursor": 2,
    "latestCursor": 8,
    "hasMore": true
}
"""
    }
}
