import { readdirSync, readFileSync, statSync, existsSync } from "node:fs";
import { join, relative } from "node:path";
import { spawnSync } from "node:child_process";

const root = process.cwd();
const errors = [];

function walk(directory, predicate) {
    const result = [];
    for (const entry of readdirSync(directory)) {
        const fullPath = join(directory, entry);
        const stat = statSync(fullPath);
        if (stat.isDirectory()) result.push(...walk(fullPath, predicate));
        else if (predicate(fullPath)) result.push(fullPath);
    }
    return result;
}

const viewFiles = walk(join(root, "Views"), (file) => file.endsWith(".cshtml"));
const jsFiles = walk(join(root, "wwwroot", "js"), (file) => file.endsWith(".js"));

for (const file of viewFiles) {
    const content = readFileSync(file, "utf8");
    const display = relative(root, file);

    if (/<script\b(?![^>]*\bsrc=)[^>]*>/i.test(content)) {
        errors.push(`${display}: inline script bulundu.`);
    }

    if (/\sstyle\s*=/i.test(content)) {
        errors.push(`${display}: inline style bulundu.`);
    }

    if (/<[^>]*\son[a-z]+\s*=/i.test(content)) {
        errors.push(`${display}: inline event handler bulundu.`);
    }

    if (/cdn\.tailwindcss\.com/i.test(content)) {
        errors.push(`${display}: Tailwind CDN kullanımı bulundu.`);
    }

    if (/\b(?:href|action)="\/[A-Za-z]/i.test(content)) {
        errors.push(`${display}: Tag Helper yerine sabit uygulama yolu kullanılıyor.`);
    }

    if (/\bapp-button-(?:primary|secondary|tertiary|danger)\b/.test(content)) {
        errors.push(`${display}: eski veya hatalı app-button sınıfı kullanılıyor.`);
    }

    if (/TempData\["(?:BasariMesaji|HataMesaji|UyariMesaji|BilgiMesaji)"\]/.test(content) &&
        !display.endsWith("_FlashMessages.cshtml")) {
        errors.push(`${display}: ortak flash mesaj bileşeni dışında doğrudan TempData mesajı kullanılıyor.`);
    }

    if (/<img\b[^>]*\bsrc=""/i.test(content)) {
        errors.push(`${display}: boş src değerine sahip görsel bulundu.`);
    }

    const ids = [...content.matchAll(/\bid="([A-Za-z][A-Za-z0-9_:-]*)"/g)]
        .map((match) => match[1])
        .filter((id) => !id.includes("@"));
    const duplicates = ids.filter((id, index) => ids.indexOf(id) !== index);
    for (const id of new Set(duplicates)) {
        errors.push(`${display}: tekrar eden statik id '${id}'.`);
    }
}

for (const file of jsFiles) {
    const result = spawnSync(process.execPath, ["--check", file], {
        encoding: "utf8"
    });

    if (result.status !== 0) {
        errors.push(`${relative(root, file)}: JavaScript sözdizimi hatası.\n${result.stderr.trim()}`);
    }
}

if (!existsSync(join(root, "wwwroot", "css", "tailwind.css"))) {
    errors.push("wwwroot/css/tailwind.css bulunamadı.");
}

if (errors.length > 0) {
    console.error("Frontend denetimi başarısız:\n- " + errors.join("\n- "));
    process.exit(1);
}

console.log(`Frontend denetimi başarılı: ${viewFiles.length} görünüm, ${jsFiles.length} JavaScript dosyası.`);
