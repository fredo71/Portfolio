// Defers the below-the-fold videos on /Game until the reader is close to them.
//
// Why this exists: <video autoplay> starts downloading the whole file the moment
// it is in the DOM, whatever preload says. On /Game the two lower clips sit six
// and eight screens down on a phone, so a reader who skims the intro and leaves
// was paying for ~5 MB of video they never saw a frame of.
//
// There is no native lazy attribute for <video> — loading="lazy" only works on
// <img> and <iframe> — so it takes an observer. A video opts in by carrying
// data-src instead of src; anything with a plain src is left alone, which is how
// the first clip stays eager.
//
// The 600px rootMargin is the point: loading starts before the clip is on screen,
// so at reading pace it is already playing on arrival. With the videos now
// faststart (moov atom first), playback begins after a few hundred KB rather than
// after the whole file.
//
// With JavaScript off, the videos keep their poster frames and never load. That is
// a deliberate, readable fallback — every poster is a meaningful still.
(function () {
    'use strict';

    if (!('IntersectionObserver' in window)) {
        // No observer: load everything rather than show posters forever.
        document.addEventListener('DOMContentLoaded', function () {
            document.querySelectorAll('video[data-src]').forEach(load);
        });
        return;
    }

    function load(video) {
        var src = video.dataset.src;
        if (!src) return;
        video.src = src;
        delete video.dataset.src;
        // autoplay alone is unreliable when src arrives after the element, so ask.
        var played = video.play();
        if (played) played.catch(function () { /* autoplay blocked — poster stays */ });
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            observer.unobserve(entry.target);
            load(entry.target);
        });
    }, { rootMargin: '600px' });

    // Blazor renders after this script runs, and re-renders on navigation, so the
    // page is watched rather than swept once. Binding is idempotent via the flag.
    function sweep() {
        document.querySelectorAll('video[data-src]').forEach(function (video) {
            if (video.dataset.lazyBound) return;
            video.dataset.lazyBound = '1';
            observer.observe(video);
        });
    }

    function start() {
        sweep();
        new MutationObserver(sweep).observe(document.body, { childList: true, subtree: true });
    }

    if (document.body) start();
    else document.addEventListener('DOMContentLoaded', start);
})();
