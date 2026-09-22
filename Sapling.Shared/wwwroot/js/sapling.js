window.sapling = (() => {
    const THEME_KEY = 'sapling-theme';

    const theme = {
        apply(mode) {
            document.documentElement.setAttribute('data-sapling-theme', mode || 'system');
        },
        read() {
            try { return localStorage.getItem(THEME_KEY) || 'system'; } catch { return 'system'; }
        },
        write(mode) {
            try { localStorage.setItem(THEME_KEY, mode); } catch { /* private mode */ }
            theme.apply(mode);
        }
    };

    const store = {
        get(key) {
            try { return localStorage.getItem(key); } catch { return null; }
        },
        set(key, value) {
            try { localStorage.setItem(key, value); } catch { /* private mode */ }
        },
        remove(key) {
            try { localStorage.removeItem(key); } catch { /* private mode */ }
        }
    };

    const carouselInstances = new WeakMap();

    const getSlideOffset = (track, domIndex) => {
        if (!track) return 0;
        const slides = track.querySelectorAll('.carousel-slide');
        if (!slides || slides.length === 0 || !slides[domIndex]) return 0;
        return slides[domIndex].offsetLeft - slides[0].offsetLeft;
    };

    const getStep = (track) => {
        if (!track) return 0;
        const slides = track.querySelectorAll('.carousel-slide');
        if (slides && slides.length >= 2) {
            const step = slides[1].offsetLeft - slides[0].offsetLeft;
            if (step > 0) return step;
        }
        const first = track.firstElementChild;
        return first ? first.getBoundingClientRect().width + 12 : 0;
    };

    const silentJump = (track, domIndex, state) => {
        if (!track) return;
        if (state) state.isJumping = true;
        const targetLeft = getSlideOffset(track, domIndex);
        const prevBehavior = track.style.scrollBehavior;
        const prevSnap = track.style.scrollSnapType;
        track.style.scrollBehavior = 'auto';
        track.style.scrollSnapType = 'none';
        try {
            track.scrollTo({ left: targetLeft, behavior: 'instant' });
        } catch {
            track.scrollLeft = targetLeft;
        }
        track.scrollLeft = targetLeft;
        void track.offsetWidth;
        track.style.scrollBehavior = prevBehavior;
        track.style.scrollSnapType = prevSnap;
        if (state) {
            setTimeout(() => { state.isJumping = false; }, 60);
        }
    };

    const scrollToSlide = (track, domIndex) => {
        if (!track) return;
        const targetLeft = getSlideOffset(track, domIndex);
        track.scrollTo({ left: targetLeft, behavior: 'smooth' });
    };

    const carousel = {
        init(track, dotNetRef, count) {
            if (!track || !count || count <= 1) return;

            carousel.destroy(track);

            const state = {
                track,
                dotNetRef,
                count,
                currentDomIndex: 1,
                isJumping: false,
                scrollTimer: null,
                frame: 0,
                cleanups: [],
                resizeObserver: null,
            };
            carouselInstances.set(track, state);

            track.classList.add('carousel-ready');
            silentJump(track, 1, state);

            track.querySelectorAll('.carousel-slide[aria-hidden="true"] a, .carousel-slide[aria-hidden="true"] button')
                .forEach(el => el.setAttribute('tabindex', '-1'));

            const onScrollEnd = () => {
                if (state.isJumping) return;
                const step = getStep(track);
                if (step <= 0) return;

                const domIndex = Math.round(track.scrollLeft / step);

                if (domIndex <= 0) {
                    silentJump(track, count, state);
                    state.currentDomIndex = count;
                    dotNetRef.invokeMethodAsync('OnScrolled', count - 1);
                } else if (domIndex >= count + 1) {
                    silentJump(track, 1, state);
                    state.currentDomIndex = 1;
                    dotNetRef.invokeMethodAsync('OnScrolled', 0);
                } else {
                    state.currentDomIndex = domIndex;
                    const logicalIndex = domIndex - 1;
                    if (logicalIndex >= 0 && logicalIndex < count) {
                        dotNetRef.invokeMethodAsync('OnScrolled', logicalIndex);
                    }
                }
            };

            let isScrollEnding = false;
            const handleScrollEnd = () => {
                if (isScrollEnding || state.isJumping) return;
                isScrollEnding = true;
                clearTimeout(state.scrollTimer);
                requestAnimationFrame(() => {
                    onScrollEnd();
                    isScrollEnding = false;
                });
            };

            if ('onscrollend' in window) {
                track.addEventListener('scrollend', handleScrollEnd);
                state.cleanups.push(() => track.removeEventListener('scrollend', handleScrollEnd));
            }

            const scrollHandler = () => {
                if (state.isJumping) return;
                clearTimeout(state.scrollTimer);
                state.scrollTimer = setTimeout(handleScrollEnd, 120);

                cancelAnimationFrame(state.frame);
                state.frame = requestAnimationFrame(() => {
                    if (state.isJumping) return;
                    const step = getStep(track);
                    if (step <= 0) return;
                    const rawIndex = Math.round(track.scrollLeft / step);
                    const logicalIndex = ((rawIndex - 1) % count + count) % count;
                    if (logicalIndex >= 0 && logicalIndex < count) {
                        dotNetRef.invokeMethodAsync('OnScrolled', logicalIndex);
                    }
                });
            };

            track.addEventListener('scroll', scrollHandler, { passive: true });
            state.cleanups.push(() => track.removeEventListener('scroll', scrollHandler));

            const pauseEvents = ['pointerdown', 'touchstart', 'mouseenter'];
            const resumeEvents = ['pointerup', 'touchend', 'mouseleave'];

            const onPause = () => dotNetRef.invokeMethodAsync('OnInteraction', true);
            const onResume = () => dotNetRef.invokeMethodAsync('OnInteraction', false);

            pauseEvents.forEach(e => {
                track.addEventListener(e, onPause, { passive: true });
                state.cleanups.push(() => track.removeEventListener(e, onPause));
            });

            resumeEvents.forEach(e => {
                track.addEventListener(e, onResume, { passive: true });
                state.cleanups.push(() => track.removeEventListener(e, onResume));
            });

            if (typeof ResizeObserver !== 'undefined') {
                state.resizeObserver = new ResizeObserver(() => {
                    const step = getStep(track);
                    if (step > 0) {
                        silentJump(track, state.currentDomIndex, state);
                    }
                });
                state.resizeObserver.observe(track);
            }
        },

        next(track) {
            const state = carouselInstances.get(track);
            if (!state || state.count <= 1) return;

            const count = state.count;
            const step = getStep(track);

            if (state.currentDomIndex >= count + 1 || (step > 0 && track.scrollLeft >= getSlideOffset(track, count) + (step / 2))) {
                silentJump(track, 1, state);
                state.currentDomIndex = 1;
            }

            const targetDomIndex = state.currentDomIndex + 1;
            state.currentDomIndex = targetDomIndex;
            scrollToSlide(track, targetDomIndex);
        },

        prev(track) {
            const state = carouselInstances.get(track);
            if (!state || state.count <= 1) return;

            const count = state.count;
            const step = getStep(track);

            if (state.currentDomIndex <= 0 || (step > 0 && track.scrollLeft <= getSlideOffset(track, 1) - (step / 2))) {
                silentJump(track, count, state);
                state.currentDomIndex = count;
            }

            const targetDomIndex = state.currentDomIndex - 1;
            state.currentDomIndex = targetDomIndex;
            scrollToSlide(track, targetDomIndex);
        },

        goTo(track, logicalIndex) {
            const state = carouselInstances.get(track);
            if (!state || state.count <= 1) return;

            const count = state.count;
            const target = ((logicalIndex % count) + count) % count;

            if (state.currentDomIndex >= count + 1) {
                silentJump(track, 1, state);
            } else if (state.currentDomIndex <= 0) {
                silentJump(track, count, state);
            }

            const targetDomIndex = target + 1;
            state.currentDomIndex = targetDomIndex;
            scrollToSlide(track, targetDomIndex);
        },

        destroy(track) {
            if (!track) return;
            const state = carouselInstances.get(track);
            if (!state) return;

            if (state.resizeObserver) {
                state.resizeObserver.disconnect();
            }
            clearTimeout(state.scrollTimer);
            cancelAnimationFrame(state.frame);
            state.cleanups.forEach(fn => fn());
            carouselInstances.delete(track);
        },

        observe(track, dotNetRef) {
            const count = track ? track.querySelectorAll('.carousel-slide:not(.carousel-clone-prev):not(.carousel-clone-next)').length : 0;
            carousel.init(track, dotNetRef, count);
        }
    };

    const sidebar = {
        init(sidebarEl, handle, dotNetRef) {
            if (!sidebarEl || !handle) return;

            const COLLAPSED_W = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--sidebar-w-collapsed')) || 68;
            const EXPANDED_W = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--sidebar-w')) || 240;
            const SNAP_THRESHOLD = (COLLAPSED_W + EXPANDED_W) / 2;
            const MIN_W = COLLAPSED_W;
            const MAX_W = 400;

            let startX = 0;
            let startW = 0;
            let dragging = false;

            const onPointerDown = (e) => {
                e.preventDefault();
                dragging = true;
                startX = e.clientX;
                startW = sidebarEl.getBoundingClientRect().width;
                sidebarEl.classList.add('dragging');
                handle.classList.add('active');
                handle.setPointerCapture(e.pointerId);
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';
            };

            const onPointerMove = (e) => {
                if (!dragging) return;
                const dx = e.clientX - startX;
                const newW = Math.max(MIN_W, Math.min(MAX_W, startW + dx));
                sidebarEl.style.width = newW + 'px';
            };

            const onPointerUp = (e) => {
                if (!dragging) return;
                dragging = false;
                sidebarEl.classList.remove('dragging');
                handle.classList.remove('active');
                document.body.style.cursor = '';
                document.body.style.userSelect = '';

                const finalW = sidebarEl.getBoundingClientRect().width;
                sidebarEl.style.width = '';

                if (finalW <= SNAP_THRESHOLD) {
                    dotNetRef.invokeMethodAsync('SetCollapsed', true);
                } else {
                    dotNetRef.invokeMethodAsync('SetCollapsed', false);
                }
            };

            handle.addEventListener('pointerdown', onPointerDown);
            handle.addEventListener('pointermove', onPointerMove);
            handle.addEventListener('pointerup', onPointerUp);
            handle.addEventListener('pointercancel', onPointerUp);
        }
    };

    const scrollToId = (id) => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });

    const print = () => window.print();

    return { theme, store, carousel, sidebar, scrollToId, print };
})();
