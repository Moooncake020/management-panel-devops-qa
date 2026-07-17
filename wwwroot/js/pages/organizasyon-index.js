(() => {
    "use strict";

    const tree = document.getElementById("orgTree");
    const searchInput = document.getElementById("orgSearch");
    const departmentSelect = document.getElementById("orgDepartmentFilter");
    const noResult = document.getElementById("orgNoResult");
    const expandButton = document.getElementById("expandOrgTree");
    const collapseButton = document.getElementById("collapseOrgTree");

    if (!tree) return;

    const normalize = (value) => (value || "").toLocaleLowerCase("tr-TR").trim();

    function directChildren(node) {
        return Array.from(node.querySelectorAll(":scope > details > .org-children > .org-tree-node"));
    }

    function updateNode(node, query, department) {
        const children = directChildren(node);
        const childVisible = children.map((child) => updateNode(child, query, department)).some(Boolean);
        const searchText = normalize(node.dataset.orgSearch);
        const nodeDepartment = normalize(node.dataset.orgDepartment);
        const ownMatch = (!query || searchText.includes(query)) && (!department || nodeDepartment === department);
        const visible = ownMatch || childVisible;

        node.hidden = !visible;

        const branch = node.querySelector(":scope > details");
        if (branch && childVisible && (query || department)) branch.open = true;
        return visible;
    }

    function applyFilters() {
        const query = normalize(searchInput?.value);
        const department = normalize(departmentSelect?.value);
        const roots = Array.from(tree.querySelectorAll(":scope > .org-root-list > .org-tree-node"));
        const anyVisible = roots.map((root) => updateNode(root, query, department)).some(Boolean);

        tree.classList.toggle("hidden", !anyVisible);
        noResult?.classList.toggle("hidden", anyVisible);
    }

    searchInput?.addEventListener("input", applyFilters);
    departmentSelect?.addEventListener("change", applyFilters);
    expandButton?.addEventListener("click", () => tree.querySelectorAll("details.org-branch").forEach((branch) => { branch.open = true; }));
    collapseButton?.addEventListener("click", () => tree.querySelectorAll("details.org-branch").forEach((branch) => { branch.open = false; }));
})();
