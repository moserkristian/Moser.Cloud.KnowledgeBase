window.askPreview = {
  _onKey: null,

  scrollHit(root) {
    if (!root) return;
    const hit = root.querySelector(".ask-hit");
    if (hit) hit.scrollIntoView({ block: "nearest", behavior: "smooth" });
  },

  activate(root, closeBtn) {
    this.deactivate();
    try {
      closeBtn?.focus?.();
    } catch (_) { /* ignore */ }
    this.scrollHit(root);
    this._onKey = (event) => {
      if (event.key !== "Escape") return;
      event.preventDefault();
      closeBtn?.click?.();
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
