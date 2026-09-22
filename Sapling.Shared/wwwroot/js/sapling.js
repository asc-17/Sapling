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

    const carousel = {
        observe(track, dotNetRef) {
            if (!track) return;
            let frame = 0;
            const report = () => {
                cancelAnimationFrame(frame);
                frame = requestAnimationFrame(() => {
                    const slide = track.firstElementChild;
                    if (!slide) return;
                    const step = slide.getBoundingClientRect().width + 12;
                    dotNetRef.invokeMethodAsync('OnScrolled', Math.round(track.scrollLeft / step));
                });
            };
            track.addEventListener('scroll', report, { passive: true });
            ['pointerdown', 'touchstart', 'mouseenter'].forEach(e =>
                track.addEventListener(e, () => dotNetRef.invokeMethodAsync('OnInteraction', true), { passive: true }));
            ['pointerup', 'touchend', 'mouseleave'].forEach(e =>
                track.addEventListener(e, () => dotNetRef.invokeMethodAsync('OnInteraction', false), { passive: true }));
        },
        goTo(track, index) {
            if (!track) return;
            const slide = track.firstElementChild;
            if (!slide) return;
            const step = slide.getBoundingClientRect().width + 12;
            track.scrollTo({ left: step * index, behavior: 'smooth' });
        }
    };

    const scrollToId = (id) => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });

    const print = () => window.print();

    return { theme, store, carousel, scrollToId, print };
})();
