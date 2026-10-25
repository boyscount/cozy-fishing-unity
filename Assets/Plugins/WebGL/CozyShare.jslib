// แชร์ภาพการ์ดปลา: มือถือเปิดหน้าต่างแชร์ของเครื่อง (LINE/IG/TikTok/Facebook) · เครื่องที่แชร์ไฟล์ไม่ได้ (คอมส่วนใหญ่) ดาวน์โหลดเป็นไฟล์ภาพแทน
mergeInto(LibraryManager.library, {
  CozyShareImage: function (ptr, len, titlePtr, textPtr, urlPtr) {
    try {
      var bytes = HEAPU8.slice(ptr, ptr + len);
      var title = UTF8ToString(titlePtr), text = UTF8ToString(textPtr), url = UTF8ToString(urlPtr);
      var blob = new Blob([bytes], { type: 'image/png' });
      var name = 'basket-of-tomorrows.png';
      var file = null;
      try { file = new File([blob], name, { type: 'image/png' }); } catch (e) {}
      var download = function () {
        var a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = name;
        document.body.appendChild(a); a.click(); setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 2000);
      };
      if (file && navigator.canShare && navigator.canShare({ files: [file] }) && navigator.share) {
        navigator.share({ files: [file], title: title, text: text + '\n' + url }).catch(function (e) { if (e && e.name !== 'AbortError') download(); });
      } else download();
      console.log('[Share] image ' + len + ' bytes · ' + (file && navigator.canShare && navigator.canShare({ files: [file] }) ? 'share sheet' : 'download'));
    } catch (e) { console.warn('[Share] ' + e); }
  },
});
