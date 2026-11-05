package com.soulcore.companion

import android.app.Application
import com.soulcore.companion.data.ChatStore
import com.soulcore.companion.data.PresenceHud
import com.soulcore.companion.notify.AppForeground
import com.soulcore.companion.notify.ConnectedNotification
import com.soulcore.companion.notify.ReplyNotification

class CompanionApp : Application() {
    override fun onCreate() {
        super.onCreate()
        // Create FGS channel early so Settings / first connect never race the first notify.
        ConnectedNotification.ensureChannel(this)
        // PROP-3: own the transcript before any screen exists, so replies that
        // land while Chat is closed are still recorded.
        ChatStore.install(this)
        // Mood word + doing-line from Host frames (same sources as desk band).
        PresenceHud.install(this)
        // FED-151: foreground tracker + reply channel + frame collector (does not touch FGS hub).
        AppForeground.init(this)
        ReplyNotification.ensureChannel(this)
        ReplyNotification.install(this)
    }
}
