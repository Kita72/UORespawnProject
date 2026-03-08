/**
 * Splash Intro Video
 * Plays the startup animation then hands control back to Blazor
 * so the static splash image is revealed seamlessly.
 */
window.initSplashVideo = function (dotnetRef) {
    const video = document.getElementById('splash-video');

    // If element is missing for any reason, tell Blazor immediately
    if (!video) {
        dotnetRef.invokeMethodAsync('OnVideoEnded');
        return;
    }

    // Single callback — fires on normal end OR on any error
    const done = () => dotnetRef.invokeMethodAsync('OnVideoEnded');

    video.addEventListener('ended', done, { once: true });
    video.addEventListener('error',  done, { once: true });

    // Programmatic play handles cases where the autoplay attribute alone
    // isn't honoured (should never happen for muted video in WebView2/WKWebView)
    const promise = video.play();
    if (promise !== undefined) {
        promise.catch(() => done());
    }
};
