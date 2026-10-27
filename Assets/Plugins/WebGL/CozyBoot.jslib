// ต่อจอโหลดหน้าเว็บ (index.html) กับการโหลดด่านครั้งแรกในเกม: แถบความคืบหน้า + ปิดจอโหลดเมื่อฉากแรกพร้อม
mergeInto(LibraryManager.library, {
  CozyBootProgress: function (p, msg) { if (window.cozyBoot) window.cozyBoot.progress(p, UTF8ToString(msg)); },
  CozyBootDone: function () { if (window.cozyBoot) window.cozyBoot.done(); },
  // ทดสอบ: ส่งผลวัดจากเกมออกมาให้สคริปต์ทดสอบอ่าน (Debug.Log ถูกตัดออกใน build จริง) → document.body.dataset.cozyNote
  CozyTestNote: function (msg) { try { document.body.dataset.cozyNote = UTF8ToString(msg); } catch (e) {} },
});
