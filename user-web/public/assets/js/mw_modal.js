/**
 * Mini Website — common popup / modal API (MwModal)
 * Requires design-system CSS from user/includes/header.php (.mw-modal-*)
 *
 * Sizes: sm | md | lg
 *
 * MwModal.open('modalId');
 * MwModal.open({ id, size, title, body, footer, static, closable });
 * MwModal.close('modalId');
 * MwModal.confirm({ title, message, size, confirmText, cancelText, onConfirm, onCancel });
 * MwModal.alert({ title, message, size, buttonText, onClose });
 * MwModal.setContent('modalId', { title, body, footer });
 */
(function (window, document) {
    'use strict';

    var OPEN_CLASS = 'is-open';
    var BODY_LOCK = 'mw-modal-open';
    var BASE_Z = 1055;
    var openStack = [];

    function normalizeSize(size) {
        size = (size || 'md').toLowerCase();
        return (size === 'sm' || size === 'lg') ? size : 'md';
    }

    function getModal(elOrId) {
        if (!elOrId) return null;
        if (typeof elOrId === 'string') return document.getElementById(elOrId);
        if (elOrId.nodeType === 1) return elOrId;
        return null;
    }

    function getRoot() {
        var root = document.getElementById('mw-modal-root');
        if (!root) {
            root = document.createElement('div');
            root.id = 'mw-modal-root';
            document.body.appendChild(root);
        }
        return root;
    }

    function lockBody() {
        document.body.classList.add(BODY_LOCK);
    }

    function unlockBody() {
        if (!document.querySelector('.mw-modal.' + OPEN_CLASS)) {
            document.body.classList.remove(BODY_LOCK);
        }
    }

    function updateZIndex(modal) {
        modal.style.zIndex = String(BASE_Z + openStack.length);
    }

    function open(idOrOptions) {
        var modal;
        var opts = {};

        if (typeof idOrOptions === 'string') {
            modal = getModal(idOrOptions);
            if (!modal) return false;
        } else if (typeof idOrOptions === 'object' && idOrOptions !== null) {
            opts = idOrOptions;
            if (opts.id) {
                modal = getModal(opts.id);
            }
            if (!modal && (opts.title || opts.body || opts.footer)) {
                modal = createModal(opts);
            }
        }

        if (!modal) return false;

        if (opts.title !== undefined) {
            var titleEl = modal.querySelector('.mw-modal-title');
            if (titleEl) titleEl.textContent = opts.title;
        }
        if (opts.body !== undefined) {
            var bodyEl = modal.querySelector('.mw-modal-body');
            if (bodyEl) bodyEl.innerHTML = opts.body;
        }
        if (opts.footer !== undefined) {
            var footerEl = modal.querySelector('.mw-modal-footer');
            if (footerEl) footerEl.innerHTML = opts.footer;
            else if (opts.footer) {
                var panel = modal.querySelector('.mw-modal-panel');
                if (panel) {
                    footerEl = document.createElement('div');
                    footerEl.className = 'mw-modal-footer';
                    footerEl.innerHTML = opts.footer;
                    panel.appendChild(footerEl);
                }
            }
        }

        if (openStack.indexOf(modal) === -1) {
            openStack.push(modal);
        }

        modal.removeAttribute('hidden');
        modal.classList.add(OPEN_CLASS);
        updateZIndex(modal);
        lockBody();

        var focusTarget = modal.querySelector('.mw-modal-close') || modal.querySelector('button, [href], input, select, textarea');
        if (focusTarget && typeof focusTarget.focus === 'function') {
            focusTarget.focus();
        }

        try {
            modal.dispatchEvent(new CustomEvent('mw-modal:opened', { bubbles: true }));
        } catch (e) { /* IE11 not supported */ }

        return true;
    }

    function close(idOrModal) {
        var modal = getModal(idOrModal);
        if (!modal) return false;

        try {
            modal.dispatchEvent(new CustomEvent('mw-modal:closed', { bubbles: true }));
        } catch (e) { /* IE11 not supported */ }

        modal.classList.remove(OPEN_CLASS);
        modal.setAttribute('hidden', 'hidden');

        openStack = openStack.filter(function (m) { return m !== modal; });

        openStack.forEach(updateZIndex);
        unlockBody();

        if (modal.getAttribute('data-mw-modal-dynamic') === 'true') {
            setTimeout(function () {
                if (!modal.classList.contains(OPEN_CLASS) && modal.parentNode) {
                    modal.parentNode.removeChild(modal);
                }
            }, 200);
        }

        return true;
    }

    function closeTop() {
        if (!openStack.length) return false;
        var modal = openStack[openStack.length - 1];
        if (modal.getAttribute('data-mw-modal-static') === 'true') return false;
        return close(modal);
    }

    function createModal(opts) {
        var id = opts.id || ('mwModal' + Date.now());
        var size = normalizeSize(opts.size);
        var title = opts.title || '';
        var subtitle = (opts.subtitle || '').trim();
        var icon = (opts.icon || '').replace(/[^a-zA-Z0-9_-]/g, '');
        var body = opts.body || '';
        var footer = opts.footer || '';
        var closable = opts.closable !== false;
        var isStatic = !!opts.static;

        var modal = document.createElement('div');
        modal.className = 'mw-modal';
        modal.id = id;
        modal.setAttribute('role', 'dialog');
        modal.setAttribute('aria-modal', 'true');
        modal.setAttribute('data-mw-modal-dynamic', 'true');
        modal.setAttribute('hidden', 'hidden');
        if (isStatic) modal.setAttribute('data-mw-modal-static', 'true');

        var titleId = id + 'Title';
        if (title) modal.setAttribute('aria-labelledby', titleId);

        var backdropStatic = isStatic ? ' data-mw-modal-static="true"' : '';
        var headerHtml = '';
        if (title || icon || closable) {
            var headerMain = '';
            if (title || icon) {
                headerMain =
                    '<div class="mw-modal-header-main">' +
                        (icon ? '<span class="mw-modal-header-icon" aria-hidden="true"><i class="fa ' + icon + '"></i></span>' : '') +
                        '<div class="mw-modal-header-text-wrap">' +
                            (title ? '<h2 class="mw-modal-title" id="' + titleId + '">' + escapeHtml(title) + '</h2>' : '') +
                        '</div>' +
                    '</div>';
            } else {
                headerMain = '<span></span>';
            }
            headerHtml =
                '<div class="mw-modal-header">' +
                    headerMain +
                    (closable ? '<button type="button" class="mw-modal-close" data-mw-modal-close aria-label="Close"><span aria-hidden="true">&times;</span></button>' : '') +
                '</div>';
        }

        var footerHtml = footer ? '<div class="mw-modal-footer">' + footer + '</div>' : '';

        modal.innerHTML =
            '<div class="mw-modal-backdrop" data-mw-modal-close' + backdropStatic + ' aria-hidden="true"></div>' +
            '<div class="mw-modal-panel mw-modal-' + size + '">' +
                headerHtml +
                '<div class="mw-modal-body">' + body + '</div>' +
                footerHtml +
            '</div>';

        getRoot().appendChild(modal);
        return modal;
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function setContent(id, content) {
        var modal = getModal(id);
        if (!modal) return false;
        content = content || {};

        if (content.title !== undefined) {
            var titleEl = modal.querySelector('.mw-modal-title');
            if (titleEl) titleEl.textContent = content.title;
        }
        if (content.body !== undefined) {
            var bodyEl = modal.querySelector('.mw-modal-body');
            if (bodyEl) bodyEl.innerHTML = content.body;
        }
        if (content.footer !== undefined) {
            var footerEl = modal.querySelector('.mw-modal-footer');
            if (footerEl) footerEl.innerHTML = content.footer;
        }
        return true;
    }

    function confirm(options) {
        options = options || {};
        var id = options.id || ('mwConfirm' + Date.now());
        var cancelText = options.cancelText || 'Cancel';
        var confirmText = options.confirmText || 'Confirm';
        var confirmClass = options.confirmClass || 'mw-btn mw-btn-save';
        var cancelClass = options.cancelClass || 'mw-btn mw-btn-cancel';

        var footer =
            '<button type="button" class="' + cancelClass + '" data-mw-modal-close>' + escapeHtml(cancelText) + '</button>' +
            '<button type="button" class="' + confirmClass + '" data-mw-confirm-ok>' + escapeHtml(confirmText) + '</button>';

        var modal = createModal({
            id: id,
            size: options.size || 'sm',
            title: options.title || 'Confirm',
            body: '<p class="!m-0">' + escapeHtml(options.message || 'Are you sure?') + '</p>',
            footer: footer,
            static: !!options.static
        });

        modal.querySelector('[data-mw-confirm-ok]').addEventListener('click', function () {
            if (typeof options.onConfirm === 'function') options.onConfirm();
            close(modal);
        });

        if (typeof options.onCancel === 'function') {
            modal.querySelectorAll('[data-mw-modal-close]').forEach(function (btn) {
                btn.addEventListener('click', options.onCancel);
            });
        }

        return open(modal);
    }

    function alert(options) {
        options = options || {};
        var id = options.id || ('mwAlert' + Date.now());
        var buttonText = options.buttonText || 'OK';

        var footer = '<button type="button" class="mw-btn mw-btn-save" data-mw-modal-close>' + escapeHtml(buttonText) + '</button>';

        var modal = createModal({
            id: id,
            size: options.size || 'sm',
            title: options.title || 'Notice',
            body: '<p class="!m-0">' + escapeHtml(options.message || '') + '</p>',
            footer: footer
        });

        if (typeof options.onClose === 'function') {
            modal.querySelectorAll('[data-mw-modal-close]').forEach(function (btn) {
                btn.addEventListener('click', options.onClose);
            });
        }

        return open(modal);
    }

    function handleClick(event) {
        var openTrigger = event.target.closest('[data-mw-modal-open]');
        if (openTrigger) {
            event.preventDefault();
            open(openTrigger.getAttribute('data-mw-modal-open'));
            return;
        }

        var closeTrigger = event.target.closest('[data-mw-modal-close]');
        if (closeTrigger) {
            if (closeTrigger.getAttribute('data-mw-modal-static') === 'true') return;
            event.preventDefault();
            var modal = closeTrigger.closest('.mw-modal');
            if (modal) close(modal);
        }
    }

    function handleKeydown(event) {
        if (event.key !== 'Escape') return;
        closeTop();
    }

    function init() {
        document.addEventListener('click', handleClick);
        document.addEventListener('keydown', handleKeydown);
    }

    window.MwModal = {
        open: open,
        close: close,
        closeTop: closeTop,
        create: createModal,
        confirm: confirm,
        alert: alert,
        setContent: setContent,
        init: init
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})(window, document);
