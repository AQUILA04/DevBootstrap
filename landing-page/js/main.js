(() => {
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
