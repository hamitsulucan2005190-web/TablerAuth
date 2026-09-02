(function () {
    function onReady(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn);
        } else {
            fn();
        }
    }

    onReady(function () {
        document.addEventListener("click", function (event) {
            var toggle = event.target.closest(".js-password-toggle");
            if (toggle) {
                event.preventDefault();
                var group = toggle.closest(".input-group");
                var input = group && group.querySelector("input");
                if (!input) return;

                var hidden = input.type === "password";
                input.type = hidden ? "text" : "password";
                toggle.setAttribute("aria-pressed", hidden ? "true" : "false");
                toggle.setAttribute("aria-label", hidden ? "Şifreyi gizle" : "Şifreyi göster");
                toggle.setAttribute("title", hidden ? "Şifreyi gizle" : "Şifreyi göster");

                var showIcon = toggle.querySelector(".js-password-show");
                var hideIcon = toggle.querySelector(".js-password-hide");
                if (showIcon && hideIcon) {
                    showIcon.classList.toggle("d-none", hidden);
                    hideIcon.classList.toggle("d-none", !hidden);
                }
                return;
            }

            var copyButton = event.target.closest(".js-copy");
            if (copyButton) {
                event.preventDefault();
                var text = copyButton.getAttribute("data-copy") || "";
                if (!text || !navigator.clipboard) return;

                navigator.clipboard.writeText(text).then(function () {
                    var original = copyButton.textContent;
                    copyButton.textContent = "Kopyalandı";
                    setTimeout(function () {
                        copyButton.textContent = original;
                    }, 1500);
                });
                return;
            }

            var themeToggle = event.target.closest(".js-theme-toggle");
            if (themeToggle) {
                event.preventDefault();
                var current = document.documentElement.getAttribute("data-bs-theme") || "light";
                var next = current === "dark" ? "light" : "dark";
                localStorage.setItem("tabler-theme", next);
                if (next === "light") {
                    document.documentElement.removeAttribute("data-bs-theme");
                } else {
                    document.documentElement.setAttribute("data-bs-theme", next);
                }
                syncThemeSwitches();
            }
        });

        var kindSelect = document.querySelector(".js-provider-kind");
        var formRoot = document.querySelector("[data-provider-form]");
        if (kindSelect && formRoot) {
            var isNew = formRoot.getAttribute("data-is-new") === "true";
            var last = { display: "", scheme: "", callback: "", scopes: "", secret: "" };

            function syncAuthorityField() {
                var option = kindSelect.selectedOptions[0];
                var required = option && option.getAttribute("data-requires-authority") === "true";
                var field = document.querySelector(".js-authority-field");
                var input = document.querySelector(".js-provider-authority");
                var label = field && field.querySelector(".form-label");
                if (!field) return;

                field.classList.toggle("d-none", !required);
                if (label) {
                    label.classList.toggle("required", !!required);
                }
                if (input) {
                    input.required = !!required;
                    if (!required) {
                        input.value = "";
                    }
                }
            }

            kindSelect.addEventListener("change", function () {
                var option = kindSelect.selectedOptions[0];
                if (!option || !option.value) {
                    syncAuthorityField();
                    return;
                }

                if (isNew) {
                    var next = {
                        display: option.value,
                        scheme: option.getAttribute("data-scheme") || "",
                        callback: option.getAttribute("data-callback") || "",
                        scopes: option.getAttribute("data-scopes") || "",
                        secret: option.getAttribute("data-secret-key") || ""
                    };

                    replaceIfSuggested(".js-provider-display-name", last.display, next.display);
                    replaceIfSuggested(".js-provider-scheme", last.scheme, next.scheme);
                    replaceIfSuggested(".js-provider-callback", last.callback, next.callback);
                    replaceIfSuggested(".js-provider-scopes", last.scopes, next.scopes);
                    replaceIfSuggested(".js-provider-secret-key", last.secret, next.secret);
                    last = next;
                }

                syncAuthorityField();
            });

            syncAuthorityField();
        }

        syncThemeSwitches();
    });

    function isDarkTheme() {
        return document.documentElement.getAttribute("data-bs-theme") === "dark";
    }

    function syncThemeSwitches() {
        var on = isDarkTheme();
        document.querySelectorAll(".js-theme-toggle").forEach(function (button) {
            button.setAttribute("aria-checked", on ? "true" : "false");
        });
    }

    function replaceIfSuggested(selector, previous, next) {
        var input = document.querySelector(selector);
        if (!input || !next) return;
        if (!input.value || input.value === previous) {
            input.value = next;
        }
    }
})();
