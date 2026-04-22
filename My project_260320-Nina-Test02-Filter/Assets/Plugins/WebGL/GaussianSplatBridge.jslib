mergeInto(LibraryManager.library, {
  OpenGaussianSplatOverlay: function (urlPtr, width, height) {
    var url = UTF8ToString(urlPtr);
    var w = width || 900;
    var h = height || 560;

    var overlay = document.getElementById('gaussian-splat-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'gaussian-splat-overlay';
      overlay.style.position = 'fixed';
      overlay.style.inset = '0';
      overlay.style.background = 'rgba(0,0,0,0.0)';
      overlay.style.display = 'none';
      overlay.style.alignItems = 'center';
      overlay.style.justifyContent = 'center';
      overlay.style.zIndex = '999999';

      var box = document.createElement('div');
      box.id = 'gaussian-splat-box';
      box.style.position = 'relative';
      box.style.background = '#111';
      box.style.border = '1px solid #333';
      box.style.borderRadius = '10px';
      box.style.padding = '8px';
      box.style.boxShadow = '0 10px 30px rgba(0,0,0,0.45)';

      var closeBtn = document.createElement('button');
      closeBtn.innerText = '✕';
      closeBtn.style.position = 'absolute';
      closeBtn.style.right = '8px';
      closeBtn.style.top = '6px';
      closeBtn.style.width = '30px';
      closeBtn.style.height = '30px';
      closeBtn.style.border = 'none';
      closeBtn.style.borderRadius = '6px';
      closeBtn.style.cursor = 'pointer';
      closeBtn.style.background = '#2b2b2b';
      closeBtn.style.color = '#fff';
      closeBtn.onclick = function () {
        overlay.style.display = 'none';
      };

      var frame = document.createElement('iframe');
      frame.id = 'gaussian-splat-iframe';
      frame.style.border = '0';
      frame.style.background = 'transparent';
      frame.style.pointerEvents = 'auto';
      frame.allow = 'fullscreen; xr-spatial-tracking;';

      box.appendChild(closeBtn);
      box.appendChild(frame);
      overlay.appendChild(box);
      document.body.appendChild(overlay);

      overlay.addEventListener('click', function (e) {
        if (e.target === overlay) {
          overlay.style.display = 'none';
        }
      });
    }

    var boxEl = document.getElementById('gaussian-splat-box');
    var frameEl = document.getElementById('gaussian-splat-iframe');
    if (boxEl && frameEl) {
      frameEl.width = w;
      frameEl.height = h;
      frameEl.src = url;
      boxEl.style.width = (w + 0) + 'px';
      boxEl.style.height = (h + 0) + 'px';
    }

    overlay.style.display = 'flex';
  },

  OpenGaussianSplatInNewTab: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    window.open(url, '_blank', 'noopener,noreferrer');
  },

  CloseGaussianSplatOverlay: function () {
    var overlay = document.getElementById('gaussian-splat-overlay');
    if (overlay) {
      overlay.style.display = 'none';
    }
  }
});
