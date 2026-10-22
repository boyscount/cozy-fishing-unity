// ต่อจอโหลดหน้าเว็บ (index.html) กับการโหลดด่านครั้งแรกในเกม: แถบความคืบหน้า + ปิดจอโหลดเมื่อฉากแรกพร้อม
mergeInto(LibraryManager.library, {
  CozyBootProgress: function (p, msg) { if (window.cozyBoot) window.cozyBoot.progress(p, UTF8ToString(msg)); },
  CozyBootDone: function () { if (window.cozyBoot) window.cozyBoot.done(); },
});
