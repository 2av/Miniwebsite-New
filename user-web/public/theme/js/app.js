/**
 * Demo App JS - Blinkit Tabs, Sticky Nav, Services read-more, AI Menu Planner
 */

/** WhatsApp: wa.me redirect mojibake for emoji on desktop/Web; web.whatsapp.com preserves UTF-8. Mobile keeps wa.me / api (works there). */
function mwOpenWhatsAppShare(phoneDigits, rawText) {
    const text = encodeURIComponent(rawText);
    const phone = String(phoneDigits || '').replace(/\D/g, '');
    const mobile = /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    if (phone) {
        if (mobile) {
            window.open(`https://wa.me/${phone}?text=${text}`, '_blank');
        } else {
            window.open(`https://web.whatsapp.com/send?phone=${phone}&text=${text}`, '_blank');
        }
    } else if (mobile) {
        window.open(`https://api.whatsapp.com/send?text=${text}`, '_blank');
    } else {
        window.open(`https://web.whatsapp.com/send?text=${text}`, '_blank');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.mw-service-read-more').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const card = btn.closest('.mw-service-card');
            if (!card) return;
            const preview = card.querySelector('.mw-service-desc-preview');
            const full = card.querySelector('.mw-service-desc-full');
            const isExpanded = full && !full.classList.contains('hidden');
            if (isExpanded) {
                full.classList.add('hidden');
                preview?.classList.remove('hidden');
                btn.textContent = 'Read more';
            } else {
                preview?.classList.add('hidden');
                full?.classList.remove('hidden');
                btn.textContent = 'Read less';
            }
        });
    });

    document.querySelectorAll('.mw-offer-read-more').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const card = btn.closest('.mw-offer-card');
            if (!card) return;
            const preview = card.querySelector('.mw-offer-desc-preview');
            const full = card.querySelector('.mw-offer-desc-full');
            const isExpanded = full && !full.classList.contains('hidden');
            if (isExpanded) {
                full.classList.add('hidden');
                preview?.classList.remove('hidden');
                btn.textContent = 'Read more';
            } else {
                preview?.classList.add('hidden');
                full?.classList.remove('hidden');
                btn.textContent = 'Read less';
            }
        });
    });

    // --- Products: Shop grid (read more / card body opens popup via handler below; no inline expand) ---

    // --- Products: Desktop inline expanded box (like Services, on image click) ---
    const productsSection = document.getElementById('mw-products');
    const productExpandedBox = document.getElementById('mw-product-expanded-box');
    const productExpandedImg = document.getElementById('mw-product-expanded-img');
    const productExpandedTitle = document.getElementById('mw-product-expanded-title');
    const productExpandedDesc = document.getElementById('mw-product-expanded-desc');
    const productExpandedMrp = document.getElementById('mw-product-expanded-mrp');
    const productExpandedPrice = document.getElementById('mw-product-expanded-price');
    const productExpandedCounter = document.getElementById('mw-product-expanded-counter');
    const productExpandedBadge = document.getElementById('mw-product-expanded-badge');
    const productExpandedSavings = document.getElementById('mw-product-expanded-savings');
    const productExpandedOfferLine = document.getElementById('mw-product-expanded-offer-line');
    const productsData = window.MW_PRODUCTS || [];
    const totalProducts = productsData.length;
    const blinkitMainEl = document.querySelector('#mw-products-blinkit .mw-blinkit-main');

    let currentProductIndex = 0;
    /** Global indices of products in the same category as the open modal (prev/next scope) */
    let modalCategoryIndices = [];

    function getIndicesForCategory(catKey) {
        const key = catKey != null ? String(catKey) : '';
        const out = [];
        let anyKeyed = false;
        for (let i = 0; i < productsData.length; i++) {
            const k = productsData[i].cat_key != null ? String(productsData[i].cat_key) : '';
            if (k !== '') anyKeyed = true;
            if (k === key) out.push(i);
        }
        if (!anyKeyed) {
            return productsData.map((_, i) => i);
        }
        return out;
    }

    function syncProductModalWidth() {
        if (!productExpandedBox) return;
        const main = blinkitMainEl || document.querySelector('#mw-products-blinkit .mw-blinkit-main');
        if (!main) return;
        const rect = main.getBoundingClientRect();
        let w = Math.round(rect.width);
        if (w < 1) return;
        const maxW = Math.max(350, window.innerWidth - 32);
        w = Math.min(w, maxW);
        productExpandedBox.style.setProperty('--mw-product-modal-width', w + 'px');
    }

    function formatFrontendPriceText(p) {
        const priceType = String(p.price_type || '').trim();
        const unit = String(p.pricing_unit || '').trim();
        const priceNum = Number(p.price) || 0;
        const priceBase = '₹' + priceNum.toLocaleString();
        const priceWithUnit = unit ? (priceBase + ' / ' + unit) : priceBase;
        const isOnRequest = priceType === 'on_request' || p.price_on_request === true || !(priceNum > 0);
        if (isOnRequest) return 'Price on Request';
        if (priceType === 'starting_from') return 'Starting ' + priceWithUnit;
        return priceWithUnit;
    }

    function getProductCtaText(p) {
        const cta = (p && p.cta_text != null ? String(p.cta_text) : '').trim();
        return cta || 'Enquire';
    }

    function updateProductExpandedContent(index) {
        if (index < 0 || index >= totalProducts || !productsData[index]) return;
        const p = productsData[index];
        const catKey = p.cat_key != null ? String(p.cat_key) : '';
        modalCategoryIndices = getIndicesForCategory(catKey);
        if (modalCategoryIndices.length === 0) modalCategoryIndices = [index];
        currentProductIndex = index;
        if (productExpandedImg) productExpandedImg.src = p.image || '';
        if (productExpandedImg) productExpandedImg.alt = p.name || '';
        if (productExpandedTitle) productExpandedTitle.textContent = p.name || '';
        if (productExpandedDesc) {
            const raw = (p.desc != null ? String(p.desc) : '') || 'Contact us for details.';
            productExpandedDesc.textContent = raw.length > 400 ? raw.slice(0, 400) : raw;
        }
        const priceType = String(p.price_type || '').trim();
        const priceOnRequest = priceType === 'on_request' || p.price_on_request === true || !(Number(p.price) > 0);
        const priceText = formatFrontendPriceText(p);
        if (productExpandedMrp) {
            if (!priceOnRequest && priceType === 'fixed_price' && p.mrp && p.mrp > p.price) {
                productExpandedMrp.textContent = '₹' + (p.mrp || 0).toLocaleString();
                productExpandedMrp.classList.remove('mw-product-expanded-mrp--empty');
            } else {
                productExpandedMrp.textContent = '';
                productExpandedMrp.classList.add('mw-product-expanded-mrp--empty');
            }
        }
        if (productExpandedPrice) {
            productExpandedPrice.textContent = priceText;
        }
        if (productExpandedOfferLine) {
            productExpandedOfferLine.textContent = priceText;
        }
        if (productExpandedBadge) {
            const cat = (p.category != null ? String(p.category) : '').trim();
            if (cat) {
                productExpandedBadge.textContent = cat;
                productExpandedBadge.classList.remove('hidden');
            } else {
                productExpandedBadge.textContent = '';
                productExpandedBadge.classList.add('hidden');
            }
        }
        if (productExpandedSavings) {
            const m = Number(p.mrp) || 0;
            const sale = Number(p.price) || 0;
            const save = !priceOnRequest && m > sale ? m - sale : 0;
            if (save > 0) {
                productExpandedSavings.textContent = '✓ You Save ₹' + save.toLocaleString();
                productExpandedSavings.classList.remove('hidden');
            } else {
                productExpandedSavings.textContent = '';
                productExpandedSavings.classList.add('hidden');
            }
        }
        const catPos = modalCategoryIndices.indexOf(index);
        if (productExpandedCounter) {
            productExpandedCounter.textContent = String(catPos >= 0 ? catPos + 1 : 1);
        }
        const counterTotalEl = document.getElementById('mw-product-expanded-counter-total');
        if (counterTotalEl) counterTotalEl.textContent = String(modalCategoryIndices.length);
        const panel = productExpandedBox?.querySelector('.mw-product-expanded-panel');
        if (panel) {
            panel.classList.toggle('mw-product-expanded-single', modalCategoryIndices.length <= 1);
        }
        if (productExpandedBox) {
            productExpandedBox.querySelectorAll('.mw-add-to-cart').forEach(btn => {
                btn.setAttribute('data-product-index', String(index));
            });
        }
        // Prev/next updates data-product-index; refresh ADD/REMOVE labels for current product
        updateCartUI();
    }

    function openProductExpandedBox(index) {
        if (index < 0 || index >= totalProducts || !productsSection || !productExpandedBox) return;
        syncProductModalWidth();
        updateProductExpandedContent(index);
        productsSection.classList.add('mw-products-expanded-active');
        productExpandedBox.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        requestAnimationFrame(() => { syncProductModalWidth(); });
    }

    let mwProductModalResizeTimer = null;
    window.addEventListener('resize', () => {
        if (!productsSection || !productsSection.classList.contains('mw-products-expanded-active')) return;
        clearTimeout(mwProductModalResizeTimer);
        mwProductModalResizeTimer = setTimeout(syncProductModalWidth, 80);
    });

    if (blinkitMainEl && typeof ResizeObserver !== 'undefined') {
        const mwBlinkitRo = new ResizeObserver(() => {
            if (productsSection && productsSection.classList.contains('mw-products-expanded-active')) {
                syncProductModalWidth();
            }
        });
        mwBlinkitRo.observe(blinkitMainEl);
    }

    function closeProductExpandedBox() {
        if (!productsSection || !productExpandedBox) return;
        productsSection.classList.remove('mw-products-expanded-active');
        productExpandedBox.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    }

    document.querySelectorAll('.mw-product-card').forEach(card => {
        card.addEventListener('click', (e) => {
            if (e.target.closest('.mw-add-to-cart')) return;
            const idx = parseInt(card.getAttribute('data-product-index'), 10);
            if (!isNaN(idx)) openProductExpandedBox(idx);
        });
    });

    const productExpandedClose = document.querySelector('.mw-product-expanded-close');
    const productExpandedPrev = document.querySelector('.mw-product-expanded-prev');
    const productExpandedNext = document.querySelector('.mw-product-expanded-next');

    if (productExpandedClose) productExpandedClose.addEventListener('click', (e) => { e.stopPropagation(); closeProductExpandedBox(); });
    const productExpandedBackdrop = document.querySelector('.mw-product-expanded-backdrop');
    if (productExpandedBackdrop) productExpandedBackdrop.addEventListener('click', closeProductExpandedBox);
    if (productExpandedPrev) productExpandedPrev.addEventListener('click', (e) => {
        e.stopPropagation();
        const pos = modalCategoryIndices.indexOf(currentProductIndex);
        if (pos > 0) updateProductExpandedContent(modalCategoryIndices[pos - 1]);
    });
    if (productExpandedNext) productExpandedNext.addEventListener('click', (e) => {
        e.stopPropagation();
        const pos = modalCategoryIndices.indexOf(currentProductIndex);
        if (pos >= 0 && pos < modalCategoryIndices.length - 1) {
            updateProductExpandedContent(modalCategoryIndices[pos + 1]);
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && productsSection?.classList.contains('mw-products-expanded-active')) closeProductExpandedBox();
    });

    // --- Videos: Load More + Modal (play within Miniwebsite) ---
    const loadMoreBtn = document.getElementById('mw-videos-load-more');
    const loadMoreWrap = document.getElementById('mw-videos-load-more-wrap');
    if (loadMoreBtn && loadMoreWrap) {
        loadMoreBtn.addEventListener('click', () => {
            document.querySelectorAll('.mw-video-item.mw-video-hidden').forEach(el => el.classList.remove('mw-video-hidden'));
            loadMoreWrap.style.display = 'none';
        });
    }

    const videoModal = document.getElementById('mw-video-modal');
    const videoModalIframe = document.getElementById('mw-video-modal-iframe');
    const videoModalClose = document.querySelector('.mw-video-modal-close');
    document.querySelectorAll('.mw-video-item[data-play-mode="iframe"]').forEach(item => {
        item.addEventListener('click', (e) => {
            const embedUrl = item.getAttribute('data-video-url') || item.getAttribute('data-video-fallback');
            if (!embedUrl || !videoModal || !videoModalIframe) return;
            videoModalIframe.src = embedUrl;
            videoModal.classList.add('open');
            videoModal.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';
        });
        item.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                item.click();
            }
        });
    });

    function closeVideoModal() {
        if (!videoModal || !videoModalIframe) return;
        videoModal.classList.remove('open');
        videoModal.setAttribute('aria-hidden', 'true');
        videoModalIframe.src = '';
        document.body.style.overflow = '';
    }
    if (videoModalClose) videoModalClose.addEventListener('click', closeVideoModal);
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && videoModal?.classList.contains('open')) closeVideoModal();
    });

    // --- Gallery: lightbox (section-width panel, viewport-centered), prev/next + swipe + arrows ---
    const galleryModal = document.getElementById('mw-gallery-modal');
    const galleryModalImg = document.getElementById('mw-gallery-modal-img');
    const galleryModalCounter = document.getElementById('mw-gallery-modal-counter');
    const galleryBackdrop = galleryModal?.querySelector('.mw-gallery-modal-backdrop');
    const galleryCloseBtn = galleryModal?.querySelector('.mw-gallery-modal-close');
    const galleryPrevBtn = galleryModal?.querySelector('.mw-gallery-modal-prev');
    const galleryNextBtn = galleryModal?.querySelector('.mw-gallery-modal-next');
    const galleryStage = galleryModal?.querySelector('.mw-gallery-modal-stage');
    const galleryItemEls = galleryModal ? Array.from(document.querySelectorAll('.mw-gallery-item')) : [];
    const galleryUrls = galleryItemEls.map((el) => el.getAttribute('data-gallery-src') || '');
    let galleryIndex = 0;
    const defaultGallerySrc = galleryModal?.getAttribute('data-default-src') || '';

    function galleryNormIndex(i) {
        const n = galleryUrls.length;
        if (n <= 0) return 0;
        return ((i % n) + n) % n;
    }

    function showGalleryAt(i) {
        if (!galleryModal || !galleryModalImg || galleryUrls.length === 0) return;
        galleryIndex = galleryNormIndex(i);
        galleryModalImg.src = galleryUrls[galleryIndex] || defaultGallerySrc;
        if (galleryModalCounter) {
            galleryModalCounter.textContent = `${galleryIndex + 1} / ${galleryUrls.length}`;
        }
    }

    function openGalleryModal(startIndex) {
        if (!galleryModal || !galleryModalImg || galleryUrls.length === 0) return;
        galleryModal.classList.toggle('mw-gallery-single', galleryUrls.length <= 1);
        showGalleryAt(startIndex);
        galleryModal.classList.add('open');
        galleryModal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
    }

    function closeGalleryModal() {
        if (!galleryModal || !galleryModalImg) return;
        galleryModal.classList.remove('open');
        galleryModal.setAttribute('aria-hidden', 'true');
        galleryModalImg.src = '';
        document.body.style.overflow = '';
    }

    if (galleryModalImg && defaultGallerySrc) {
        galleryModalImg.addEventListener('error', function onGalleryImgErr() {
            this.onerror = null;
            this.src = defaultGallerySrc;
        });
    }

    galleryItemEls.forEach((item, idx) => {
        item.addEventListener('click', () => openGalleryModal(idx));
        item.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                openGalleryModal(idx);
            }
        });
    });

    if (galleryBackdrop) galleryBackdrop.addEventListener('click', closeGalleryModal);
    if (galleryCloseBtn) galleryCloseBtn.addEventListener('click', (e) => { e.stopPropagation(); closeGalleryModal(); });
    if (galleryPrevBtn) galleryPrevBtn.addEventListener('click', (e) => { e.stopPropagation(); showGalleryAt(galleryIndex - 1); });
    if (galleryNextBtn) galleryNextBtn.addEventListener('click', (e) => { e.stopPropagation(); showGalleryAt(galleryIndex + 1); });

    let galleryTouchX = null;
    if (galleryStage) {
        galleryStage.addEventListener('touchstart', (e) => {
            if (e.changedTouches?.length) galleryTouchX = e.changedTouches[0].clientX;
        }, { passive: true });
        galleryStage.addEventListener('touchend', (e) => {
            if (galleryTouchX == null || !e.changedTouches?.length) return;
            const dx = e.changedTouches[0].clientX - galleryTouchX;
            galleryTouchX = null;
            if (Math.abs(dx) < 50 || galleryUrls.length <= 1) return;
            if (dx > 0) showGalleryAt(galleryIndex - 1);
            else showGalleryAt(galleryIndex + 1);
        }, { passive: true });
    }

    document.addEventListener('keydown', (e) => {
        if (!galleryModal?.classList.contains('open')) return;
        if (e.key === 'Escape') {
            closeGalleryModal();
            return;
        }
        if (galleryUrls.length <= 1) return;
        if (e.key === 'ArrowLeft') {
            e.preventDefault();
            showGalleryAt(galleryIndex - 1);
        }
        if (e.key === 'ArrowRight') {
            e.preventDefault();
            showGalleryAt(galleryIndex + 1);
        }
    });

    // --- Sticky Nav: Smooth scroll + active state ---
    const navItems = document.querySelectorAll('.mw-sticky-nav .mw-nav-item');
    const sections = ['mw-hero', 'mw-services', 'mw-offers', 'mw-products', 'mw-gallery', 'mw-pay'];

    navItems.forEach(item => {
        item.addEventListener('click', (e) => {
            const targetId = item.getAttribute('href');
            if (targetId && targetId.startsWith('#')) {
                const el = document.querySelector(targetId);
                if (el) {
                    e.preventDefault();
                    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        });
    });

    // Update active nav item based on scroll position
    function updateActiveNav() {
        const scrollY = window.scrollY || window.pageYOffset;
        let activeId = 'mw-hero';
        const offset = 120;

        for (let i = sections.length - 1; i >= 0; i--) {
            const el = document.getElementById(sections[i]);
            if (el) {
                const top = el.getBoundingClientRect().top + scrollY;
                if (scrollY >= top - offset) {
                    activeId = sections[i];
                    break;
                }
            }
        }

        navItems.forEach(item => {
            const section = item.getAttribute('data-section');
            item.classList.toggle('active', section === activeId);
        });
    }

    let scrollTicking = false;
    window.addEventListener('scroll', () => {
        if (!scrollTicking) {
            requestAnimationFrame(() => {
                updateActiveNav();
                scrollTicking = false;
            });
            scrollTicking = true;
        }
    }, { passive: true });
    updateActiveNav(); // Initial state

    // --- Shop Cart: Add multiple products, then Share on WhatsApp ---
    const products = window.MW_PRODUCTS || [];
    const whatsappNum = (window.MW_WHATSAPP_NUMBER || '').replace(/[^0-9]/g, '');
    const cartBar = document.getElementById('mw-shop-cart-bar');
    const cartCountEl = document.getElementById('mw-cart-count');
    const cartShareBtn = document.getElementById('mw-cart-share-wa');
    const floatingWaBtn = document.getElementById('mw-floating-wa-btn');

    const cart = []; // { index, name, price, qty, price_on_request? }

    function isInCart(idx) {
        return cart.some(c => c.index === idx);
    }

    function updateCartUI() {
        const totalQty = cart.reduce((s, i) => s + i.qty, 0);
        if (cartCountEl) cartCountEl.textContent = totalQty;
        if (cartBar) {
            if (totalQty > 0) {
                cartBar.classList.remove('hidden');
                if (floatingWaBtn) floatingWaBtn.classList.add('hidden');
            } else {
                cartBar.classList.add('hidden');
                if (floatingWaBtn) floatingWaBtn.classList.remove('hidden');
            }
        }
        // Update CTA/REMOVE button text
        document.querySelectorAll('.mw-add-to-cart').forEach(btn => {
            const idx = btn.getAttribute('data-product-index');
            const n = idx != null ? parseInt(idx, 10) : NaN;
            let label = 'Enquire';
            if (!isNaN(n) && products[n]) {
                label = getProductCtaText(products[n]);
            }
            if (idx != null && isInCart(n)) label = 'REMOVE';
            const span = btn.querySelector('.mw-cart-btn-label');
            if (span) span.textContent = label;
            else btn.textContent = label;
        });
    }

    function addToCart(productIndex) {
        const idx = parseInt(productIndex, 10);
        if (isNaN(idx) || !products[idx]) return;
        const p = products[idx];
        const existing = cart.find(c => c.index === idx);
        if (existing) existing.qty += 1;
        else {
            cart.push({
                index: idx,
                name: p.name,
                price: p.price,
                qty: 1,
                price_type: p.price_type,
                pricing_unit: p.pricing_unit,
                cta_text: getProductCtaText(p),
                price_on_request: p.price_on_request === true || !(Number(p.price) > 0),
            });
        }
        updateCartUI();
    }

    function removeFromCart(productIndex) {
        const idx = parseInt(productIndex, 10);
        const i = cart.findIndex(c => c.index === idx);
        if (i >= 0) {
            cart.splice(i, 1);
            updateCartUI();
        }
    }

    document.querySelectorAll('.mw-add-to-cart').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const idx = btn.getAttribute('data-product-index');
            if (idx == null) return;
            const n = parseInt(idx, 10);
            if (isInCart(n)) removeFromCart(idx);
            else addToCart(idx);
        });
    });

    if (cartShareBtn && whatsappNum) {
        cartShareBtn.addEventListener('click', () => {
            if (cart.length === 0) return;
            let msg = 'Hi, I wanted to Enquire related to below mentioned:\n\n';
            cart.forEach((item, idx) => {
                const itemPriceText = formatFrontendPriceText(item);
                msg += `Item: ${item.name}\n`;
                msg += `Pricing: ${itemPriceText}`;
                if (item.qty > 1) msg += ` x ${item.qty}`;
                if (idx < cart.length - 1) msg += '\n\n';
            });
            msg += '\n\nPlease share details.';
            mwOpenWhatsAppShare(whatsappNum, msg);
        });
    }

    // Ensure initial button labels use saved CTA text.
    updateCartUI();

    // --- Share Profile Section ---
    const shareUrl = window.MW_SHARE_URL || '';
    const heroName = window.MW_HERO_NAME || '';
    const location = window.MW_LOCATION || '';
    const phone = window.MW_PHONE || '';
    const email = window.MW_EMAIL || '';
    const shareWaInput = document.getElementById('mw-share-wa-input');
    const shareWaBtn = document.getElementById('mw-share-wa-btn');
    const saveContactBtn = document.getElementById('mw-save-contact-btn');
    const shareLinkBtn = document.getElementById('mw-share-link-btn');

    function showToast(msg) {
        const t = document.createElement('div');
        t.className = 'fixed bottom-24 left-1/2 -translate-x-1/2 bg-heading text-bgbase px-4 py-2 rounded-theme text-sm font-medium shadow-lg z-[60] transition-opacity duration-300';
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; setTimeout(() => t.remove(), 300); }, 2000);
    }

    document.querySelectorAll('.mw-offer-wa-cta').forEach((el) => {
        el.addEventListener('click', () => {
            const phone = (el.getAttribute('data-phone') || '').replace(/\D/g, '');
            const msg = el.getAttribute('data-msg') || '';
            if (!phone || !msg) return;
            mwOpenWhatsAppShare(phone, msg);
        });
    });

    if (shareWaBtn && shareUrl) {
        shareWaBtn.addEventListener('click', () => {
            const num = (shareWaInput?.value || '').replace(/[^0-9]/g, '');
            const businessName = (heroName || 'Our business').trim();
            const locationParts = String(location || '').split(',').map((part) => part.trim()).filter(Boolean);
            const areaCity = locationParts.length >= 2 ? `${locationParts[0]}, ${locationParts[1]}` : (locationParts[0] || 'your area');
            const msg = `Hello 😊
This is ${businessName} from ${areaCity}.

We have created a MiniWebsite of our business.
Now you can easily check our products/services and offers online here:
👉 ${shareUrl}

If you need anything, just send a message on WhatsApp 👍

Thanks a lot for your support 🙏`;
            mwOpenWhatsAppShare(num, msg);
        });
    }

    function escapeVcardValue(s) {
        if (s == null || s === '') return '';
        return String(s)
            .replace(/\\/g, '\\\\')
            .replace(/\r\n|\r|\n/g, '\n')
            .replace(/\n/g, '\\n')
            .replace(/;/g, '\\;')
            .replace(/,/g, '\\,');
    }

    /** RFC 2426 line folding (required for Windows Contacts / Outlook). */
    function foldVcardLine(line) {
        const s = String(line || '');
        if (s.length <= 75) return s;
        const parts = [s.slice(0, 75)];
        let pos = 75;
        while (pos < s.length) {
            parts.push(' ' + s.slice(pos, pos + 74));
            pos += 74;
        }
        return parts.join('\r\n');
    }

    function vcardLine(prop, rawValue, typeParams) {
        const raw = String(rawValue ?? '').trim();
        if (!raw) return '';
        const val = escapeVcardValue(raw);
        const paramStr = typeParams ? `;${typeParams}` : '';
        return `${prop}${paramStr}:${val}`;
    }

    function finalizeVcardBody(lines) {
        return lines
            .map((line) => foldVcardLine(String(line)))
            .filter((line) => line.trim() !== '')
            .join('\r\n') + '\r\n';
    }

    function normalizeComparableUrl(raw) {
        const s = String(raw || '').trim();
        if (!s) return '';
        const full = /^https?:\/\//i.test(s) ? s : `https://${s}`;
        try {
            const u = new URL(full);
            const host = u.hostname.toLowerCase().replace(/^www\./, '');
            const path = u.pathname.replace(/\/+$/, '');
            const query = u.search.replace(/^\?/, '');
            return `${host}${path}${query ? `?${query}` : ''}`;
        } catch (_) {
            return full.toLowerCase().replace(/\/+$/, '');
        }
    }

    function isGoogleMapsUrl(raw) {
        return /(?:google\.com\/maps|maps\.google\.com|goo\.gl\/maps|maps\.app\.goo\.gl)/i.test(String(raw || ''));
    }

    function buildMwVcardLines(v) {
        const fnRaw = String(v.fn || '').trim();
        const orgRaw = String(v.org || '').trim();
        const titleRaw = String(v.title || '').trim();
        const businessCategoryRaw = String(v.businessCategory || '').trim();
        const cell = String(v.telCell || '').replace(/\s/g, '');
        const wa = String(v.telWhatsapp || '').replace(/\s/g, '');
        const primary = cell || wa;
        const em = String(v.email || '').trim();
        const urlProf = String(v.urlProfile || shareUrl || '').trim();
        const urlWeb = String(v.urlWebsite || '').trim();
        const mapUrl = String(v.mapUrl || '').trim();
        const waMe = String(v.waMe || '').trim();
        const social = v.social && typeof v.social === 'object' ? v.social : {};
        const socialMap = [
            { key: 'facebook', type: 'facebook', label: 'Facebook' },
            { key: 'instagram', type: 'instagram', label: 'Instagram' },
            { key: 'linkedin', type: 'linkedin', label: 'LinkedIn' },
            { key: 'twitter', type: 'x-twitter', label: 'Twitter' },
            { key: 'youtube', type: 'youtube', label: 'YouTube' },
            { key: 'pinterest', type: 'pinterest', label: 'Pinterest' }
        ];
        const nFamRaw = String(v.nFamily || '').trim();
        const nGivRaw = String(v.nGiven || '').trim();
        const adr = v.adr && typeof v.adr === 'object' ? v.adr : {};
        const streetRaw = String(adr.street || '').trim();
        const localityRaw = String(adr.locality || '').trim();
        const regionRaw = String(adr.region || '').trim();
        const postalRaw = String(adr.postal || '').trim();
        const countryRaw = String(adr.country || '').trim();

        const lines = [
            'BEGIN:VCARD',
            'VERSION:3.0',
            'PRODID:-//MiniWebsite//EN'
        ];

        const seenUrls = new Set();
        const rememberUrl = (raw) => {
            const key = normalizeComparableUrl(raw);
            if (!key || seenUrls.has(key)) return false;
            seenUrls.add(key);
            return true;
        };

        const displayFn = fnRaw || orgRaw || 'Contact';
        if (nGivRaw || nFamRaw) {
            lines.push(`N:${escapeVcardValue(nFamRaw)};${escapeVcardValue(nGivRaw)};;;`);
        } else {
            lines.push(`N:;${escapeVcardValue(displayFn)};;;`);
        }
        lines.push(`FN:${escapeVcardValue(displayFn)}`);

        const orgLine = vcardLine('ORG', orgRaw);
        if (orgLine) lines.push(orgLine);

        const titleLine = vcardLine('TITLE', titleRaw);
        if (titleLine) lines.push(titleLine);

        if (businessCategoryRaw && businessCategoryRaw !== titleRaw) {
            const roleLine = vcardLine('ROLE', businessCategoryRaw);
            if (roleLine) lines.push(roleLine);
        }

        if (primary) {
            lines.push(`TEL;TYPE=CELL;TYPE=VOICE:${primary}`);
        }
        if (wa && wa !== primary) {
            lines.push(`TEL;TYPE=CELL:${wa}`);
        }
        if (em) {
            const emailLine = vcardLine('EMAIL', em, 'TYPE=INTERNET');
            if (emailLine) lines.push(emailLine);
        }

        if (urlProf && rememberUrl(urlProf)) {
            const profileLine = vcardLine('URL', urlProf, 'TYPE=WORK');
            if (profileLine) lines.push(profileLine);
        }

        let resolvedMapUrl = mapUrl;
        if (!resolvedMapUrl && urlWeb && isGoogleMapsUrl(urlWeb)) {
            resolvedMapUrl = urlWeb;
        }

        if (urlWeb) {
            const fullWeb = /^https?:\/\//i.test(urlWeb) ? urlWeb : `https://${urlWeb}`;
            if (!isGoogleMapsUrl(fullWeb) && rememberUrl(fullWeb)) {
                const webLine = vcardLine('URL', fullWeb);
                if (webLine) lines.push(webLine);
            }
        }

        if (resolvedMapUrl) {
            const fullMap = /^https?:\/\//i.test(resolvedMapUrl) ? resolvedMapUrl : `https://${resolvedMapUrl}`;
            if (rememberUrl(fullMap)) {
                const mapLine = vcardLine('URL', fullMap, 'TYPE=pref');
                if (mapLine) lines.push(mapLine);
            }
        }

        if (streetRaw || localityRaw || regionRaw || postalRaw || countryRaw) {
            const adrValue = `;;${escapeVcardValue(streetRaw)};${escapeVcardValue(localityRaw)};${escapeVcardValue(regionRaw)};${escapeVcardValue(postalRaw)};${escapeVcardValue(countryRaw)}`;
            lines.push(`ADR;TYPE=WORK:${adrValue}`);
        }

        const noteParts = [];
        const noteRaw = String(v.note || '').trim();
        if (noteRaw) noteParts.push(noteRaw);

        socialMap.forEach(({ key, type, label }) => {
            const raw = String(social[key] || '').trim();
            if (!raw) return;
            const full = /^https?:\/\//i.test(raw) ? raw : `https://${raw}`;
            if (rememberUrl(full)) {
                const socialUrlLine = vcardLine('URL', full);
                if (socialUrlLine) lines.push(socialUrlLine);
            }
        });

        if (waMe && rememberUrl(waMe)) {
            const waLine = vcardLine('URL', waMe);
            if (waLine) lines.push(waLine);
        }

        const noteText = noteParts.join('\n');
        if (noteText) {
            const noteLine = vcardLine('NOTE', noteText);
            if (noteLine) lines.push(noteLine);
        }

        lines.push('END:VCARD');
        return lines;
    }

    if (saveContactBtn) {
        saveContactBtn.addEventListener('click', async () => {
            const v = window.MW_VCARD;
            let lines;
            if (v && typeof v === 'object' && Object.keys(v).length) {
                lines = buildMwVcardLines(v);
            } else if (heroName) {
                const e = escapeVcardValue;
                const hero = String(heroName).trim();
                lines = [
                    'BEGIN:VCARD',
                    'VERSION:3.0',
                    'PRODID:-//MiniWebsite//EN',
                    `N:;${e(hero)};;;`,
                    `FN:${e(hero)}`,
                    phone ? `TEL;TYPE=CELL;TYPE=VOICE:${phone.replace(/\s/g, '')}` : '',
                    email ? `EMAIL;TYPE=INTERNET:${e(email)}` : '',
                    shareUrl ? `URL;TYPE=WORK:${e(shareUrl)}` : '',
                    'END:VCARD'
                ].filter(Boolean);
            } else {
                showToast('No contact data');
                return;
            }

            const vcardBody = finalizeVcardBody(lines);
            const blob = new Blob([vcardBody], { type: 'text/vcard;charset=utf-8' });
            const baseName = (v && v.org) ? v.org : ((v && v.fn) ? v.fn : heroName);
            const fileName = `${String(baseName).replace(/\s+/g, '_').replace(/[^\w.-]/g, '') || 'contact'}.vcf`;
            const file = new File([blob], fileName, { type: 'text/vcard;charset=utf-8' });

            if (navigator.canShare && navigator.share) {
                try {
                    if (navigator.canShare({ files: [file] })) {
                        await navigator.share({ files: [file], title: String(baseName) });
                        showToast('Contact ready — open the file to save');
                        return;
                    }
                } catch (err) {
                    if (err && err.name === 'AbortError') return;
                }
            }

            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(a.href);
            showToast('Contact downloaded — open the .vcf file to save');
        });
    }

    if (shareLinkBtn && shareUrl) {
        shareLinkBtn.addEventListener('click', () => {
            if (navigator.share) {
                navigator.share({
                    title: heroName + ' - Profile',
                    text: 'Check out this profile',
                    url: shareUrl
                }).then(() => showToast('Shared!')).catch(() => copyAndToast());
            } else {
                copyAndToast();
            }
        });
    }

    function copyAndToast() {
        if (navigator.clipboard?.writeText) {
            navigator.clipboard.writeText(shareUrl).then(() => showToast('Link copied!')).catch(() => showToast(shareUrl));
        } else {
            showToast(shareUrl);
        }
    }

    // --- Blinkit Product UI Tab Switching Logic ---
    const categoryItems = document.querySelectorAll('.mw-cat-item');
    const productGrids = document.querySelectorAll('.product-category-grid');

    categoryItems.forEach(item => {
        item.addEventListener('click', () => {
            // Remove active from all tabs
            categoryItems.forEach(c => c.classList.remove('active'));
            // Add active to clicked
            item.classList.add('active');

            // Hide all grids
            productGrids.forEach(g => {
                g.classList.add('hidden');
                g.classList.remove('active');
            });

            // Show target grid
            const targetCat = item.getAttribute('data-cat');
            const targetGrid = document.getElementById('grid-' + targetCat);
            if (targetGrid) {
                targetGrid.classList.remove('hidden');
                targetGrid.classList.add('active');
            }
        });
    });

    // --- Gemini AI Menu Logic ---
    const apiKey = window.MW_AI_API_KEY || '';
    const inputEl = document.getElementById('ai-event-input');
    const generateBtn = document.getElementById('ai-generate-btn');
    const loadingEl = document.getElementById('ai-loading');
    const errorEl = document.getElementById('ai-error');
    const resultContainer = document.getElementById('ai-result-container');
    const resultContent = document.getElementById('ai-result-content');
    const bookBtn = document.getElementById('ai-book-btn');
    const whatsappNumber = window.MW_WHATSAPP_NUMBER || '1234567890';

    async function fetchWithRetry(url, options, maxRetries = 5) {
        let delays = [1000, 2000, 4000, 8000, 16000];
        for (let i = 0; i < maxRetries; i++) {
            try {
                const response = await fetch(url, options);
                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                return await response.json();
            } catch (error) {
                if (i === maxRetries - 1) throw error;
                await new Promise(res => setTimeout(res, delays[i]));
            }
        }
    }

    if (generateBtn) {
        generateBtn.addEventListener('click', async () => {
            const prompt = inputEl ? inputEl.value.trim() : '';
            if (!prompt) {
                if (errorEl) {
                    errorEl.textContent = "Please describe your event first!";
                    errorEl.classList.remove('hidden');
                }
                return;
            }

            if (errorEl) errorEl.classList.add('hidden');
            if (resultContainer) resultContainer.classList.add('hidden');
            generateBtn.classList.add('hidden');
            if (loadingEl) loadingEl.classList.remove('hidden');

            const systemInstruction = "You are an AI assistant for Executive Chef Olivia Murray. Generate a custom, elegant 3-course gourmet menu based on the user's event. Respond in a warm, appetizing tone. Format the output cleanly with headings. Keep it concise.";
            const payload = { contents: [{ parts: [{ text: prompt }] }], systemInstruction: { parts: [{ text: systemInstruction }] } };

            try {
                const data = await fetchWithRetry(`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-09-2025:generateContent?key=${apiKey}`, {
                    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload)
                });
                const text = data.candidates?.[0]?.content?.parts?.[0]?.text;
                if (text) {
                    if (resultContent) {
                        resultContent.innerHTML = text.replace(/\*\*(.*?)\*\*/g, '<strong class="text-heading font-medium">$1</strong>').replace(/\n/g, '<br>');
                    }
                    if (resultContainer) resultContainer.classList.remove('hidden');
                    const waRaw = `Hi Chef Olivia! I used your AI Menu Planner. Request: ${prompt}\n\nMenu:\n${text.replace(/\*\*/g, '')}`;
                    if (bookBtn) bookBtn.onclick = () => mwOpenWhatsAppShare(whatsappNumber.replace(/^\+/, ''), waRaw);
                } else throw new Error("No menu generated.");
            } catch (error) {
                if (errorEl) {
                    errorEl.textContent = "Oops! Error generating menu. Please try again.";
                    errorEl.classList.remove('hidden');
                }
            } finally {
                if (loadingEl) loadingEl.classList.add('hidden');
                generateBtn.classList.remove('hidden');
                generateBtn.innerHTML = "Regenerate Menu ✨";
            }
        });
    }
});
