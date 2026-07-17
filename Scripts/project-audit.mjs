import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative, basename, dirname } from "node:path";

const root = process.cwd();
const errors = [];
const warnings = [];

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

function count(text, character) {
    return [...text].filter((item) => item === character).length;
}

const csharpFiles = [
    ...walk(join(root, "Controllers"), (file) => file.endsWith(".cs")),
    ...walk(join(root, "Models"), (file) => file.endsWith(".cs")),
    ...walk(join(root, "Services"), (file) => file.endsWith(".cs")),
    ...walk(join(root, "Security"), (file) => file.endsWith(".cs")),
    ...walk(join(root, "Validation"), (file) => file.endsWith(".cs")),
    join(root, "Program.cs")
];

for (const file of csharpFiles) {
    const content = readFileSync(file, "utf8");
    if (count(content, "{") !== count(content, "}")) {
        errors.push(`${relative(root, file)}: süslü parantez dengesi bozuk.`);
    }
}

const program = readFileSync(join(root, "Program.cs"), "utf8");
for (const required of [
    "AutoValidateAntiforgeryTokenAttribute",
    "UseExceptionHandler",
    "UseStatusCodePagesWithReExecute",
    "UseHttpsRedirection",
    "UseForwardedHeaders",
    "Content-Security-Policy",
    "UseRateLimiter"
]) {
    if (!program.includes(required)) {
        errors.push(`Program.cs: zorunlu güvenlik/çalışma öğesi bulunamadı: ${required}.`);
    }
}

const appSettings = JSON.parse(readFileSync(join(root, "appsettings.json"), "utf8"));
if (appSettings.Jwt?.Key) {
    errors.push("appsettings.json: Jwt:Key açık metin olarak tutulmamalıdır.");
}
if (appSettings.BootstrapAdmin?.Password) {
    errors.push("appsettings.json: BootstrapAdmin:Password açık metin olarak tutulmamalıdır.");
}
if ((appSettings.ConnectionStrings?.DefaultConnection || "").includes("localdb")) {
    warnings.push("appsettings.json yerel geliştirme bağlantısı içeriyor; üretimde environment variable veya secret manager ile geçersiz kılınmalıdır.");
}

const controllerDirectory = join(root, "Controllers");
const controllers = new Map();
for (const file of walk(controllerDirectory, (entry) => entry.endsWith("Controller.cs"))) {
    const content = readFileSync(file, "utf8");
    const controller = basename(file, ".cs").replace(/Controller$/, "");
    const actions = new Set(
        [...content.matchAll(/public\s+(?:async\s+)?(?:Task<)?IActionResult>?\s+(\w+)\s*\(/g)]
            .map((match) => match[1])
    );
    controllers.set(controller, actions);
}

const views = walk(join(root, "Views"), (file) => file.endsWith(".cshtml"));
for (const file of views) {
    const content = readFileSync(file, "utf8");
    const folder = basename(dirname(file));
    const defaultController = ["Shared", "Components"].includes(folder)
        ? null
        : folder;

    for (const match of content.matchAll(/<(?:a|form)\b[^>]*?asp-action="([^"]+)"[^>]*>/gs)) {
        const tag = match[0];
        const action = match[1];
        const controllerMatch = tag.match(/asp-controller="([^"]+)"/);
        const controller = controllerMatch?.[1] || defaultController;
        if (!controller || controller.includes("@") || action.includes("@")) continue;

        if (!controllers.has(controller) || !controllers.get(controller).has(action)) {
            errors.push(`${relative(root, file)}: ${controller}.${action} için controller action bulunamadı.`);
        }
    }
}

for (const requiredFile of [
    "Views/Shared/_Layout.cshtml",
    "Views/Shared/_PublicLayout.cshtml",
    "Views/Shared/_FlashMessages.cshtml",
    "wwwroot/css/tailwind.css",
    "wwwroot/css/site.css",
    "wwwroot/js/site.js",
    "wwwroot/lib/jquery/dist/jquery.min.js",
    "wwwroot/lib/jquery-validation/dist/jquery.validate.min.js",
    "wwwroot/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"
]) {
    if (!existsSync(join(root, requiredFile))) {
        errors.push(`${requiredFile} bulunamadı.`);
    }
}

if (warnings.length > 0) {
    console.warn("Proje denetimi uyarıları:\n- " + warnings.join("\n- "));
}

if (errors.length > 0) {
    console.error("Proje denetimi başarısız:\n- " + errors.join("\n- "));
    process.exit(1);
}

console.log(`Proje denetimi başarılı: ${csharpFiles.length} C# dosyası, ${controllers.size} controller ve ${views.length} görünüm.`);
