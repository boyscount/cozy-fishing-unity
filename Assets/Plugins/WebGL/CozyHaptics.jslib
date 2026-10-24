// สั่นมือถือ (Android Chrome รองรับ navigator.vibrate · iPhone Safari ไม่รองรับ → ไม่ทำอะไร)
mergeInto(LibraryManager.library, {
  CozyVibrate: function (ms) {
    try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) {}
  },
});
