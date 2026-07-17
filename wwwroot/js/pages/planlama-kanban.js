(() => {
    "use strict";

    const board = document.getElementById("kanbanBoard");
    const statusRegion = document.getElementById("kanbanStatus");
    const tokenInput = document.querySelector("#kanbanAntiForgeryForm input[name='__RequestVerificationToken']");

    if (!board || !tokenInput) {
        return;
    }

    const updateUrl = board.dataset.updateUrl;
    let draggedCard = null;
    let dragOrigin = null;
    let dragOriginNextSibling = null;
    let requestInProgress = false;

    const announce = (message, tone = "info") => {
        if (!statusRegion) {
            return;
        }

        statusRegion.textContent = message;
        statusRegion.dataset.tone = tone;
        statusRegion.hidden = false;

        window.clearTimeout(announce.timeoutId);
        announce.timeoutId = window.setTimeout(() => {
            statusRegion.hidden = true;
        }, 5000);
    };

    const allowedStatuses = (card) => (card.dataset.allowedStatuses || "")
        .split("|")
        .map((value) => value.trim())
        .filter(Boolean);

    const updateColumnState = (column) => {
        if (!column) {
            return;
        }

        const cards = column.querySelectorAll(":scope .kanban-card");
        const count = column.closest(".kanban-column")?.querySelector("[data-column-count]");
        const empty = column.querySelector(":scope > [data-empty-state]");

        if (count) {
            count.textContent = String(cards.length);
        }

        if (empty) {
            empty.hidden = cards.length > 0;
        }
    };

    const rebuildStatusSelect = (card, statuses, currentStatus) => {
        const select = card.querySelector("[data-status-select]");
        const normalized = Array.isArray(statuses) && statuses.length > 0
            ? statuses
            : [currentStatus];

        card.dataset.allowedStatuses = normalized.join("|");
        card.draggable = normalized.length > 1;

        if (!select) {
            return;
        }

        select.replaceChildren();

        normalized.forEach((status) => {
            const option = document.createElement("option");
            option.value = status;
            option.textContent = status;
            option.selected = status === currentStatus;
            select.append(option);
        });
    };

    const restoreCard = (card, origin, nextSibling, oldStatus) => {
        if (!origin) {
            return;
        }

        if (nextSibling && nextSibling.parentElement === origin) {
            origin.insertBefore(card, nextSibling);
        } else {
            origin.append(card);
        }

        card.dataset.currentStatus = oldStatus;
        const select = card.querySelector("[data-status-select]");
        if (select) {
            select.value = oldStatus;
        }

        updateColumnState(origin);
    };

    const sendStatusUpdate = async (card, targetStatus, origin, nextSibling) => {
        if (requestInProgress) {
            announce("Önceki durum değişikliği tamamlanıyor.", "warning");
            return false;
        }

        const oldStatus = card.dataset.currentStatus || "";

        if (!targetStatus || targetStatus === oldStatus) {
            return true;
        }

        requestInProgress = true;
        card.classList.add("is-updating");

        const body = new URLSearchParams({
            id: card.dataset.taskId || "",
            yeniDurum: targetStatus,
            __RequestVerificationToken: tokenInput.value
        });

        try {
            const response = await fetch(updateUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "X-Requested-With": "XMLHttpRequest"
                },
                credentials: "same-origin",
                body
            });

            let payload = {};
            try {
                payload = await response.json();
            } catch {
                payload = {};
            }

            if (response.status === 401) {
                restoreCard(card, origin, nextSibling, oldStatus);
                announce(payload.mesaj || "Oturumunuz sona erdi. Giriş sayfasına yönlendiriliyorsunuz.", "danger");
                window.location.assign(
                    typeof payload.girisUrl === "string"
                        ? payload.girisUrl
                        : "/Auth/Login");
                return false;
            }

            if (!response.ok || payload.basarili !== true) {
                restoreCard(card, origin, nextSibling, oldStatus);
                announce(payload.mesaj || "Görev durumu güncellenemedi.", "danger");
                return false;
            }

            card.dataset.currentStatus = payload.durum || targetStatus;
            rebuildStatusSelect(card, payload.izinliDurumlar, payload.durum || targetStatus);
            announce(payload.mesaj || "Görev durumu güncellendi.", "success");
            return true;
        } catch {
            restoreCard(card, origin, nextSibling, oldStatus);
            announce("Sunucuya ulaşılamadı. Görev eski konumuna alındı.", "danger");
            return false;
        } finally {
            requestInProgress = false;
            card.classList.remove("is-updating");
        }
    };

    const moveCard = async (card, targetStatus) => {
        const origin = card.parentElement;
        const nextSibling = card.nextElementSibling;
        const target = board.querySelector(`[data-dropzone][data-status="${CSS.escape(targetStatus)}"]`);
        const oldStatus = card.dataset.currentStatus || "";

        if (targetStatus === oldStatus) {
            const select = card.querySelector("[data-status-select]");
            if (select) {
                select.value = oldStatus;
            }
            return;
        }

        if (!allowedStatuses(card).includes(targetStatus)) {
            const select = card.querySelector("[data-status-select]");
            if (select) {
                select.value = oldStatus;
            }
            announce(`'${oldStatus}' durumundan '${targetStatus}' durumuna geçişe izin verilmiyor.`, "warning");
            return;
        }

        if (target) {
            target.insertBefore(card, target.querySelector("[data-empty-state]"));
            updateColumnState(origin);
            updateColumnState(target);
        }

        const succeeded = await sendStatusUpdate(card, targetStatus, origin, nextSibling);

        if (succeeded && !target) {
            card.remove();
        }

        board.querySelectorAll("[data-dropzone]").forEach(updateColumnState);
    };

    board.addEventListener("change", (event) => {
        const select = event.target.closest("[data-status-select]");
        if (!select) {
            return;
        }

        const card = select.closest(".kanban-card");
        if (card) {
            moveCard(card, select.value);
        }
    });

    board.addEventListener("dragstart", (event) => {
        const card = event.target.closest(".kanban-card");
        if (!card || card.draggable !== true || requestInProgress) {
            event.preventDefault();
            return;
        }

        draggedCard = card;
        dragOrigin = card.parentElement;
        dragOriginNextSibling = card.nextElementSibling;
        card.classList.add("is-dragging");
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", card.dataset.taskId || "");
    });

    board.addEventListener("dragover", (event) => {
        const dropzone = event.target.closest("[data-dropzone]");
        if (!dropzone || !draggedCard) {
            return;
        }

        const targetStatus = dropzone.dataset.status || "";
        const permitted = allowedStatuses(draggedCard).includes(targetStatus);
        dropzone.classList.toggle("is-blocked", !permitted);

        if (permitted) {
            event.preventDefault();
            event.dataTransfer.dropEffect = "move";
            dropzone.classList.add("is-drag-over");
        }
    });

    board.addEventListener("dragleave", (event) => {
        const dropzone = event.target.closest("[data-dropzone]");
        if (dropzone && !dropzone.contains(event.relatedTarget)) {
            dropzone.classList.remove("is-drag-over", "is-blocked");
        }
    });

    board.addEventListener("drop", async (event) => {
        const dropzone = event.target.closest("[data-dropzone]");
        if (!dropzone || !draggedCard) {
            return;
        }

        event.preventDefault();
        const card = draggedCard;
        const targetStatus = dropzone.dataset.status || "";

        board.querySelectorAll("[data-dropzone]").forEach((zone) => {
            zone.classList.remove("is-drag-over", "is-blocked");
        });

        if (!allowedStatuses(card).includes(targetStatus)) {
            announce(`Bu görev '${targetStatus}' sütununa taşınamaz.`, "warning");
            return;
        }

        dropzone.insertBefore(card, dropzone.querySelector("[data-empty-state]"));
        updateColumnState(dragOrigin);
        updateColumnState(dropzone);
        await sendStatusUpdate(card, targetStatus, dragOrigin, dragOriginNextSibling);
        board.querySelectorAll("[data-dropzone]").forEach(updateColumnState);
    });

    board.addEventListener("dragend", () => {
        if (draggedCard) {
            draggedCard.classList.remove("is-dragging");
        }
        board.querySelectorAll("[data-dropzone]").forEach((zone) => {
            zone.classList.remove("is-drag-over", "is-blocked");
        });
        draggedCard = null;
        dragOrigin = null;
        dragOriginNextSibling = null;
    });

    board.querySelectorAll("[data-dropzone]").forEach(updateColumnState);
})();
