(() => {
    const $ = (s, root = document) => root.querySelector(s);

    // ===== Theme =====
    const themeRoot = document.documentElement;
    const themeToggle = $("[data-theme-toggle]");
    const THEME_KEY = "site-theme"; // 'light' | 'dark' | 'system'

    function getSystemTheme() {
        return window.matchMedia?.("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function applyTheme(theme) {
        themeRoot.dataset.theme = theme;
        if (!themeToggle) return;
        const effective = theme === "system" ? getSystemTheme() : theme;
        themeToggle.innerHTML =
            effective === "dark"
                ? '<i class="fa-solid fa-moon" aria-hidden="true"></i>'
                : '<i class="fa-solid fa-sun" aria-hidden="true"></i>';
    }

    function initTheme() {
        const saved = localStorage.getItem(THEME_KEY) || "system";
        applyTheme(saved);

        const mq = window.matchMedia?.("(prefers-color-scheme: dark)");
        mq?.addEventListener?.("change", () => {
            if ((themeRoot.dataset.theme || "system") === "system")
                applyTheme("system");
        });
    }

    themeToggle?.addEventListener("click", () => {
        const current = themeRoot.dataset.theme || "system";
        const next =
            current === "system"
                ? "light"
                : current === "light"
                    ? "dark"
                    : "system";
        localStorage.setItem(THEME_KEY, next);
        applyTheme(next);
    });

    initTheme();

    // ===== Mobile collapse =====
    const toggleBtn = $("[data-nav-toggle]");
    const collapse = $("[data-nav-collapse]");

    function setCollapse(open) {
        if (!collapse || !toggleBtn) return;
        collapse.dataset.open = open ? "true" : "false";
        toggleBtn.setAttribute("aria-expanded", String(open));
    }

    toggleBtn?.addEventListener("click", () => {
        const isOpen = collapse?.dataset.open === "true";
        setCollapse(!isOpen);
    });

    // ===== Dropdown =====
    const dropdown = $("[data-dropdown]");
    const dropdownBtn = $("[data-dropdown-btn]");
    const dropdownMenu = $("[data-dropdown-menu]");

    function setDropdown(open) {
        if (!dropdown || !dropdownBtn) return;
        dropdown.dataset.open = open ? "true" : "false";
        dropdownBtn.setAttribute("aria-expanded", String(open));
    }

    dropdownBtn?.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        const isOpen = dropdown?.dataset.open === "true";
        setDropdown(!isOpen);
    });

    document.addEventListener("click", (e) => {
        if (!dropdown) return;
        if (dropdown.contains(e.target)) return;
        if (dropdown.dataset.open === "true") setDropdown(false);
    });

    document.addEventListener("keydown", (e) => {
        if (e.key !== "Escape") return;
        if (dropdown?.dataset.open === "true") setDropdown(false);
        if (collapse?.dataset.open === "true") setCollapse(false);
    });

    dropdownBtn?.addEventListener("keydown", (e) => {
        if (e.key !== "ArrowDown") return;
        if (dropdown?.dataset.open !== "true") setDropdown(true);
        const firstItem = dropdownMenu?.querySelector('[role="menuitem"]');
        firstItem?.focus?.();
    });

    const mqMobile = window.matchMedia("(max-width: 992px)");
    mqMobile.addEventListener?.("change", () => {
        if (!mqMobile.matches) {
            delete collapse?.dataset.open;
            toggleBtn?.setAttribute("aria-expanded", "false");
            setDropdown(false);
        } else {
            setCollapse(false);
            setDropdown(false);
        }
    });

    if (mqMobile.matches) {
        setCollapse(false);
        setDropdown(false);
    }
})();
