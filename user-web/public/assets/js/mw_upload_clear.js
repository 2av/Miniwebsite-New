/**
 * Shared clear (×) control for website-builder image uploads.
 * Used with .delImg buttons — same pattern as payment-details.php QR uploads.
 */
(function (window, document) {
    'use strict';

    function resolveEl(target) {
        if (!target) return null;
        if (typeof target === 'string') return document.querySelector(target);
        if (target.nodeType === 1) return target;
        return null;
    }

    window.mwShowUploadClear = function (target) {
        var btn = resolveEl(target);
        if (btn) btn.classList.add('is-visible');
    };

    window.mwHideUploadClear = function (target) {
        var btn = resolveEl(target);
        if (btn) btn.classList.remove('is-visible');
    };

    window.mwClearUploadedImage = function (options) {
        options = options || {};
        if (typeof options.onBeforeClear === 'function' && options.onBeforeClear() === false) {
            return;
        }

        if (options.processedKey && Object.prototype.hasOwnProperty.call(window, options.processedKey)) {
            window[options.processedKey] = null;
        }

        var fileInput = resolveEl(options.fileInput);
        if (fileInput) {
            try { fileInput.value = ''; } catch (e) { /* ignore */ }
        }

        var hidden = resolveEl(options.hiddenField);
        if (hidden) hidden.value = '';

        var img = resolveEl(options.previewImg);
        if (img && options.placeholderSrc) {
            img.src = options.placeholderSrc;
        }
        if (img && options.resetImgStyle) {
            Object.keys(options.resetImgStyle).forEach(function (key) {
                img.style[key] = options.resetImgStyle[key];
            });
        }
        if (img && options.hidePreview) {
            img.style.display = 'none';
        }

        var wrap = resolveEl(options.previewWrap);
        if (wrap && options.resetWrapStyle) {
            Object.keys(options.resetWrapStyle).forEach(function (key) {
                wrap.style[key] = options.resetWrapStyle[key];
            });
        }

        var placeholder = resolveEl(options.placeholderEl);
        if (placeholder) placeholder.style.display = '';

        var fileNameEl = resolveEl(options.fileNameEl);
        if (fileNameEl && options.emptyFileName !== undefined) {
            fileNameEl.textContent = options.emptyFileName;
            fileNameEl.removeAttribute('title');
        }

        mwHideUploadClear(options.clearBtn);

        if (typeof options.onAfterClear === 'function') {
            options.onAfterClear();
        }
    };
})(window, document);
