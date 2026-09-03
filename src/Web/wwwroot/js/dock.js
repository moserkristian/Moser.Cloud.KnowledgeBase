(function () {
    "use strict";

    var LOCK = "is-icon-locked";
    var FLAG = "dockIconLocked";
    var STORE = "dockIconLocked";

    function desktop() {
        return window.matchMedia("(min-width: 769px)").matches;
    }

    function routeFrom(href) {
        try {
            var path = new URL(href || "", document.baseURI).pathname.replace(/\/+$/, "");
            return path || "/";
        } catch (e) {
            return "/";
        }
    }

    function currentRoute() {
        var path = location.pathname.replace(/\/+$/, "");
        return path || "/";
    }

    function storedLock() {
        try {
            var v = Number(sessionStorage.getItem(STORE) || 0);
            return v > 0 && (Date.now() - v) < 4000;
        } catch (e) {
            return false;
        }
    }

    function isLocked() {
        return document.documentElement.dataset[FLAG] === "1";
    }

    function setLocked(on) {
        var root = document.documentElement;
        var dock = document.querySelector(".dock");
        if (on) {
            try { sessionStorage.setItem(STORE, String(Date.now())); } catch (e) { /* ignore */ }
            root.dataset[FLAG] = "1";
            if (dock) {
                dock.classList.add(LOCK);
            }
        } else {
            try { sessionStorage.removeItem(STORE); } catch (e) { /* ignore */ }
            delete root.dataset[FLAG];
            if (dock) {
                dock.classList.remove(LOCK);
            }
        }
    }

    function dockHovered(dock) {
        return !!(dock && dock.matches(":hover"));
    }

    // Click-lock is only for the unbroken hover after a route click.
    // Live :hover (not last pointer coords) is the source of truth so a
    // missed pointerleave or Blazor swap cannot leave the rail stuck thin.
    function unlockIfPointerLeft() {
        if (!isLocked()) {
            return;
        }
        if (!desktop() || !dockHovered(document.querySelector(".dock"))) {
            setLocked(false);
        }
    }

    function restoreLockAfterNav() {
        if (!desktop()) {
            setLocked(false);
            return;
        }
        if (!storedLock()) {
            return;
        }
        if (dockHovered(document.querySelector(".dock"))) {
            setLocked(true);
            return;
        }
        // New .dock after enhanced nav is not :hover until the next frame.
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                if (!storedLock()) {
                    return;
                }
                if (dockHovered(document.querySelector(".dock"))) {
                    setLocked(true);
                } else {
                    setLocked(false);
                }
            });
        });
    }

    // Lock only on click (not pointerdown): collapsing the rail on pointerdown
    // shrinks the hit target under the cursor so the following click misses Status/etc.
    function onNavClick(e) {
        if (!desktop()) {
            return;
        }
        var dock = e.currentTarget;
        var item = e.target.closest("a.dock-link, a.dock-brand");
        if (!item || !dock.contains(item)) {
            return;
        }
        if (routeFrom(item.getAttribute("href")) === currentRoute()) {
            return;
        }
        setLocked(true);
    }

    function bind(dock) {
        if (!dock || dock.dataset.iconLockBound === "1") {
            return;
        }
        dock.dataset.iconLockBound = "1";
        dock.addEventListener("click", onNavClick);
        dock.addEventListener("pointerleave", function (e) {
            if (e.relatedTarget && e.relatedTarget.closest && e.relatedTarget.closest(".dock")) {
                return;
            }
            requestAnimationFrame(unlockIfPointerLeft);
        });
    }

    var watching = false;

    function init() {
        bind(document.querySelector(".dock"));
        restoreLockAfterNav();
        var shell = document.querySelector(".shell");
        if (shell && !watching) {
            watching = true;
            new MutationObserver(init).observe(shell, { childList: true });
        }
    }

    document.addEventListener("pointermove", unlockIfPointerLeft, { passive: true });
    window.addEventListener("blur", function () {
        setLocked(false);
    });

    init();
    document.addEventListener("enhancedload", init);
    if (window.Blazor && typeof Blazor.addEventListener === "function") {
        Blazor.addEventListener("enhancedload", init);
    }
})();
