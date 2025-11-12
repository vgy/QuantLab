import { initializeUI, setupEventListeners } from "./ui.js";

document.addEventListener("DOMContentLoaded", async () => {
  await initializeUI();
  setupEventListeners();
});
