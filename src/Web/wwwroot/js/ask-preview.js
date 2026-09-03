window.askPreview = {
  _onKey: null,

  scrollHit(root) {
    if (!root) return;
    const hit = root.querySelector(".ask-hit");
    if (hit) hit.scrollIntoView({ block: "nearest", behavior: "smooth" });
  },

  focusable(root) {
    if (!root) return [];
    return Array.from(
      root.querySelectorAll(
        'a[href], button:not([disabled]), textarea, input, select, iframe, [tabindex]:not([tabindex="-1"])'
      )
    ).filter((el) => el.offsetParent !== null || el === document.activeElement);
  },

  trapTab(root, event) {
    if (!root || event.key !== "Tab") return;
    const items = this.focusable(root);
    if (items.length === 0) return;
    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      last.focus();
      event.preventDefault();
      return;
    }
    if (!event.shiftKey && document.activeElement === last) {
      first.focus();
      event.preventDefault();
    }
  },

  activate(root, closeBtn) {
    this.deactivate();
    try {
      closeBtn?.focus?.();
    } catch (_) { /* ignore */ }
    this.scrollHit(root);
    this._onKey = (event) => {
      if (event.key === "Escape") {
        event.preventDefault();
        closeBtn?.click?.();
        return;
      }
      this.trapTab(root, event);
    };
    document.addEventListener("keydown", this._onKey, true);
  },

  deactivate() {
    if (this._onKey) {
      document.removeEventListener("keydown", this._onKey, true);
      this._onKey = null;
    }
  }
};
