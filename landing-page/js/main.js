(() => {
  // Centralized Store CTA — PLACEHOLDER until Partner Center Product ID exists.
  // Replace microsoftStoreUrl with https://apps.microsoft.com/detail/<PRODUCT_ID>
  // and set microsoftStoreUrlIsPlaceholder to false when the listing is live.
  const storeLinks = {
    microsoftStoreUrl: "https://apps.microsoft.com/search?query=StackPilot%20OptimizeSolux",
    microsoftStoreUrlIsPlaceholder: true,
    githubMsiUrl: "https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-1.2.4-x64.msi"
  };

  document.querySelectorAll("[data-store-cta]").forEach((el) => {
    el.setAttribute("href", storeLinks.microsoftStoreUrl);
    if (storeLinks.microsoftStoreUrlIsPlaceholder) {
      el.setAttribute("title", "Lien Store placeholder — fiche pas encore publiée");
    }
  });

  const glow = document.querySelector(".cursor-glow");
  if (glow && window.matchMedia("(pointer: fine)").matches) {
    let visible = false;
    window.addEventListener("pointermove", (event) => {
      glow.style.transform = `translate(${event.clientX}px, ${event.clientY}px)`;
      if (!visible) {
        glow.style.opacity = "1";
        visible = true;
      }
    });
    window.addEventListener("pointerleave", () => {
      glow.style.opacity = "0";
      visible = false;
    });
  }

  const reveals = document.querySelectorAll(".reveal");
  if (!("IntersectionObserver" in window)) {
    reveals.forEach((el) => el.classList.add("is-visible"));
    return;
  }

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.16, rootMargin: "0px 0px -8% 0px" }
  );

  reveals.forEach((el) => observer.observe(el));
})();
