(() => {
  const reveals = document.querySelectorAll(".reveal");
  if (!("IntersectionObserver" in window)) {
    reveals.forEach((el) => el.classList.add("is-in"));
  } else {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-in");
          observer.unobserve(entry.target);
        });
      },
      { threshold: 0.14, rootMargin: "0px 0px -6% 0px" }
    );
    reveals.forEach((el) => observer.observe(el));
  }

  const runway = document.querySelector(".hero-runway");
  if (!runway || window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    return;
  }

  const marks = runway.querySelectorAll("li.checked span");
  marks.forEach((dot, i) => {
    dot.style.transform = "scale(0)";
    dot.style.transition = `transform 0.35s cubic-bezier(0.22, 1, 0.36, 1) ${0.28 + i * 0.07}s`;
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        dot.style.transform = "scale(1)";
      });
    });
  });
})();
