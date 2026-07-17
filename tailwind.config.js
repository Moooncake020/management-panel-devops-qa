/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Views/**/*.cshtml",
        "./wwwroot/js/**/*.js"
    ],
    theme: {
        extend: {
            boxShadow: {
                xs: "0 1px 2px 0 rgb(15 23 42 / 0.05)"
            }
        }
    },
    plugins: []
};
