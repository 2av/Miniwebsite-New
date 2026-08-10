/**
 * Common image upload with crop (Cropper.js).
 * Requires: Cropper.js, MwModal (assets/js/mw_modal.js), modal HTML from common/image_upload_crop_modal.php.
 */
(function() {
    'use strict';

    var cropperInstance = null;
    var currentOptions = null;
    var currentFile = null;
    var handlersBound = false;

    function $(sel) {
        if (typeof window.jQuery !== 'undefined') {
            return window.jQuery(sel);
        }
        return null;
    }

    function setFieldValue(selector, value) {
        if (!selector) return;
        var el = typeof selector === 'string' ? document.querySelector(selector) : selector;
        if (el) el.value = value;
    }

    function updatePreview() {
        if (!cropperInstance || !currentOptions) return;
        var cw = (currentOptions.cropWidth != null) ? currentOptions.cropWidth : 512;
        var ch = (currentOptions.cropHeight != null) ? currentOptions.cropHeight : 512;
        var canvas = cropperInstance.getCroppedCanvas({
            width: cw,
            height: ch,
            imageSmoothingEnabled: true,
            imageSmoothingQuality: 'high'
        });
        if (canvas) {
            var box = document.getElementById('cropPreviewBox');
            var dim = document.getElementById('cropCroppedDimensions');
            var titleEl = document.getElementById('cropPreviewTitle');
            if (box) {
                box.innerHTML = '';
                var img = document.createElement('img');
                img.src = canvas.toDataURL('image/png');
                img.style.width = img.style.height = '100%';
                img.style.objectFit = 'contain';
                box.appendChild(img);
            }
            if (dim) dim.textContent = cw + ' × ' + ch + ' px';
            if (titleEl) titleEl.textContent = 'Preview (' + cw + '×' + ch + ')';
        }
    }

    function updateZoomSlider() {
        if (!cropperInstance) return;
        var data = cropperInstance.getData();
        var zoom = (data && data.zoom != null) ? data.zoom : 1;
        var s = document.getElementById('cropZoomSlider');
        if (s) s.value = Math.min(Math.max(zoom, 0), 3);
    }

    function closeCropModal() {
        if (window.MwModal && typeof window.MwModal.close === 'function') {
            window.MwModal.close('imageCropModal');
        }
    }

    function initCropper() {
        if (typeof Cropper === 'undefined') {
            if (window.MwModal && window.MwModal.alert) {
                window.MwModal.alert({ title: 'Error', message: 'Image cropping library failed to load. Please refresh the page.' });
            } else {
                alert('Image cropping library failed to load. Please refresh the page.');
            }
            closeCropModal();
            return;
        }
        var modal = document.getElementById('imageCropModal');
        var opts = (modal && modal._mwCropOptions) ? modal._mwCropOptions : currentOptions;
        if (opts) currentOptions = opts;
        var img = document.getElementById('imageToCrop');
        if (!img || !img.src) return;
        if (cropperInstance) {
            cropperInstance.destroy();
            cropperInstance = null;
        }
        setTimeout(function() {
            try {
                var aspectRatio = (opts && opts.aspectRatio != null) ? opts.aspectRatio : 1;
                cropperInstance = new Cropper(img, {
                    aspectRatio: aspectRatio,
                    viewMode: 1,
                    dragMode: 'move',
                    autoCropArea: 0.8,
                    restore: false,
                    guides: true,
                    center: true,
                    highlight: false,
                    cropBoxMovable: true,
                    cropBoxResizable: true,
                    zoomable: true,
                    scalable: true,
                    rotatable: true,
                    minCropBoxWidth: 100,
                    minCropBoxHeight: 100,
                    ready: function() { updatePreview(); updateZoomSlider(); },
                    crop: function() { updatePreview(); }
                });
                if (cropperInstance.cropper && cropperInstance.cropper.addEventListener) {
                    cropperInstance.cropper.addEventListener('zoom', function() {
                        updateZoomSlider();
                        setTimeout(updatePreview, 100);
                    });
                }
            } catch (err) {
                console.error(err);
                if (window.MwModal && window.MwModal.alert) {
                    window.MwModal.alert({ title: 'Error', message: 'Failed to initialize cropper.' });
                } else {
                    alert('Failed to initialize cropper.');
                }
            }
        }, 100);
    }

    function destroyCropper() {
        if (cropperInstance) {
            cropperInstance.destroy();
            cropperInstance = null;
        }
        var img = document.getElementById('imageToCrop');
        if (img) img.removeAttribute('src');
        currentOptions = null;
        currentFile = null;
        var modal = document.getElementById('imageCropModal');
        if (modal) modal._mwCropOptions = null;
        var box = document.getElementById('cropPreviewBox');
        if (box) box.innerHTML = '';
        var s = document.getElementById('cropZoomSlider');
        if (s) s.value = 1;
    }

    function bindModalHandlers() {
        if (handlersBound) return;
        var modal = document.getElementById('imageCropModal');
        if (!modal) return;
        handlersBound = true;

        modal.addEventListener('mw-modal:opened', function() {
            initCropper();
        });
        modal.addEventListener('mw-modal:closed', function() {
            destroyCropper();
        });

        var zoomSlider = document.getElementById('cropZoomSlider');
        if (zoomSlider) {
            zoomSlider.addEventListener('input', function() {
                if (cropperInstance) cropperInstance.zoomTo(parseFloat(zoomSlider.value));
            });
        }

        function bindBtn(id, fn) {
            var btn = document.getElementById(id);
            if (btn) btn.addEventListener('click', fn);
        }

        bindBtn('cropRotateLeft', function(e) {
            e.preventDefault();
            if (cropperInstance) { cropperInstance.rotate(-90); setTimeout(updatePreview, 100); }
        });
        bindBtn('cropRotateRight', function(e) {
            e.preventDefault();
            if (cropperInstance) { cropperInstance.rotate(90); setTimeout(updatePreview, 100); }
        });
        bindBtn('cropZoomIn', function(e) {
            e.preventDefault();
            if (cropperInstance) { cropperInstance.zoom(0.1); updateZoomSlider(); setTimeout(updatePreview, 100); }
        });
        bindBtn('cropZoomOut', function(e) {
            e.preventDefault();
            if (cropperInstance) { cropperInstance.zoom(-0.1); updateZoomSlider(); setTimeout(updatePreview, 100); }
        });
        bindBtn('cropFlipH', function(e) {
            e.preventDefault();
            if (cropperInstance) {
                var d = cropperInstance.getImageData();
                cropperInstance.scaleX(-(d.scaleX || -1));
                setTimeout(updatePreview, 100);
            }
        });
        bindBtn('cropFlipV', function(e) {
            e.preventDefault();
            if (cropperInstance) {
                var d = cropperInstance.getImageData();
                cropperInstance.scaleY(-(d.scaleY || -1));
                setTimeout(updatePreview, 100);
            }
        });
        bindBtn('cropResetCrop', function(e) {
            e.preventDefault();
            if (cropperInstance) {
                cropperInstance.reset();
                var s = document.getElementById('cropZoomSlider');
                if (s) s.value = 1;
                setTimeout(updatePreview, 100);
            }
        });

        var saveBtn = document.getElementById('cropAndSaveBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', function() {
                if (!cropperInstance || !currentOptions) return;
                var cropW = (currentOptions.cropWidth != null) ? currentOptions.cropWidth : 512;
                var cropH = (currentOptions.cropHeight != null) ? currentOptions.cropHeight : 512;
                var canvas = cropperInstance.getCroppedCanvas({
                    width: cropW,
                    height: cropH,
                    imageSmoothingEnabled: true,
                    imageSmoothingQuality: 'high'
                });
                if (!canvas) {
                    if (currentOptions.onError) currentOptions.onError('Could not crop image.');
                    return;
                }

                // Capture callbacks before close — mw-modal:closed runs destroyCropper() which clears currentOptions.
                var opts = currentOptions;
                var onSuccessCb = opts.onSuccess;
                var onErrorCb = opts.onError;

                if (opts.method === 'base64') {
                    var outputFormat = (opts.outputFormat || 'jpeg').toLowerCase();
                    var mime = outputFormat === 'png' ? 'image/png' : 'image/jpeg';
                    var quality = outputFormat === 'png' ? undefined : 0.95;
                    var dataUrl = quality !== undefined ? canvas.toDataURL(mime, quality) : canvas.toDataURL(mime);
                    var base64 = dataUrl.replace(/^data:image\/\w+;base64,/, '');
                    setFieldValue(opts.hiddenField, base64);
                    var previewEl = opts.previewSelector
                        ? document.querySelector(opts.previewSelector)
                        : null;
                    if (previewEl) {
                        previewEl.setAttribute('src', dataUrl);
                        previewEl.style.display = '';
                    }
                    if (opts.spanSelector) {
                        var spanEl = document.querySelector(opts.spanSelector);
                        if (spanEl) spanEl.style.display = 'none';
                    }
                    closeCropModal();
                    if (onSuccessCb) onSuccessCb(base64);
                    return;
                }

                if (opts.method === 'upload' && opts.uploadUrl) {
                    canvas.toBlob(function(blob) {
                        if (!blob) {
                            if (onErrorCb) onErrorCb('Failed to process image.');
                            return;
                        }
                        var formData = new FormData();
                        var fieldName = opts.uploadFieldName || 'image';
                        var fileName = (currentFile && currentFile.name) ? currentFile.name : 'cropped.png';
                        formData.append(fieldName, blob, fileName);

                        var loading = null;
                        if (opts.showLoading) {
                            loading = document.createElement('div');
                            loading.className = 'alert alert-info';
                            loading.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;';
                            loading.textContent = 'Uploading...';
                            document.body.appendChild(loading);
                        }

                        var xhr = new XMLHttpRequest();
                        xhr.open('POST', opts.uploadUrl, true);
                        xhr.timeout = opts.timeout || 30000;
                        xhr.onload = function() {
                            if (loading) loading.remove();
                            var response = null;
                            try { response = JSON.parse(xhr.responseText); } catch (e) {}
                            if (xhr.status >= 200 && xhr.status < 300) {
                                closeCropModal();
                                if (onSuccessCb) onSuccessCb(response);
                            } else {
                                var msg = (response && response.message) ? response.message : 'Upload failed. Please try again.';
                                if (onErrorCb) onErrorCb(msg);
                                else if (window.MwModal && window.MwModal.alert) window.MwModal.alert({ title: 'Upload failed', message: msg });
                                else alert(msg);
                            }
                        };
                        xhr.onerror = function() {
                            if (loading) loading.remove();
                            var msg = 'Upload failed. Please try again.';
                            if (onErrorCb) onErrorCb(msg);
                            else alert(msg);
                        };
                        xhr.send(formData);
                    }, 'image/png', 0.95);
                }
            });
        }
    }

    window.ImageCropUpload = {
        open: function(file, options) {
            if (!file || !file.type || !file.type.match(/^image\//)) {
                if (options.onError) options.onError('Please select an image file.');
                else alert('Please select an image file.');
                return;
            }
            if (file.size > 10 * 1024 * 1024) {
                if (options.onError) options.onError('Image must be 10MB or less.');
                else alert('Image must be 10MB or less.');
                return;
            }
            if (!window.MwModal || typeof window.MwModal.open !== 'function') {
                if (options.onError) options.onError('Modal system not loaded. Please refresh the page.');
                else alert('Modal system not loaded. Please refresh the page.');
                return;
            }

            currentFile = file;
            currentOptions = options || {};
            currentOptions.method = currentOptions.method || 'base64';

            var titleEl = document.getElementById('imageCropModalTitle');
            if (titleEl && currentOptions.title) {
                titleEl.textContent = currentOptions.title;
            }

            bindModalHandlers();

            var optsForThisOpen = Object.assign({}, currentOptions);
            var reader = new FileReader();
            reader.onload = function(e) {
                var modal = document.getElementById('imageCropModal');
                if (modal) modal._mwCropOptions = optsForThisOpen;
                var img = document.getElementById('imageToCrop');
                if (img) img.src = e.target.result;
                window.MwModal.open('imageCropModal');
            };
            reader.readAsDataURL(file);
        }
    };
})();
