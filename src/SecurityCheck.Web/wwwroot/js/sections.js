// En stängd <details> döljer innehållet, så sektionen måste öppnas innan webbläsaren scrollar till ankaret.
document.addEventListener(
  "click",
  (event) => {
    const link = event.target.closest('a[href^="#"]');

    if (!link) {
      return;
    }

    const target = document.getElementById(link.getAttribute("href").slice(1));
    if (target instanceof HTMLDetailsElement) {
      target.open = true;
    }
  },
  true, // Körs i capture-fasen.
);
