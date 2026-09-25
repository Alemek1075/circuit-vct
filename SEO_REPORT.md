# SEO-аудит публічного архіву

25.09.2026 перевірено `https://alemek1075.github.io/circuit-vct/` за допомогою Lighthouse 13.5.0 у Microsoft Edge, мобільний режим, категорія SEO. [Повний JSON-звіт](docs/lighthouse-seo.json) містить кінцеву адресу, час запуску й результати окремих перевірок.

**Результат SEO: 100/100.** Пройшли перевірки доступності для індексації, заголовка, опису, HTTP статусу, зрозумілих посилань, `alt`, canonical і коректності `robots.txt`. Lighthouse не виявив провалених автоматичних SEO перевірок.

Команда відтворення:

```powershell
$env:CHROME_PATH='C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
npx -y lighthouse@13.5.0 'https://alemek1075.github.io/circuit-vct/' --only-categories=seo --output=json --output-path='tests/.tmp/lighthouse-seo.json' --chrome-flags='--headless --no-sandbox' --quiet
```

JSON-звіт створено без `runtimeError`, але команда повернула exit code 1 через `EPERM` під час видалення тимчасового профілю Edge після аудиту. PageSpeed Insights API відхилив окрему спробу кодом 429, тому оцінку PageSpeed не наведено. Оцінка стосується лише цієї публічної статичної сторінки й автоматичних SEO правил Lighthouse; вона не доводить ранжування чи індексацію пошуковиком.
