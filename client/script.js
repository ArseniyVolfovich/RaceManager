const menuButton = document.querySelector(".menu-toggle");
const menu = document.querySelector(".main-nav");

if (menuButton && menu) {
  menuButton.addEventListener("click", () => {
    const isOpen = menu.classList.toggle("is-open");
    menuButton.setAttribute("aria-expanded", String(isOpen));
  });
}


document.documentElement.classList.add("page-ready");

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

const supportedPhonePattern = /^(?:\+375\s(?:24|25|29|33|44)\s\d{3}(?:\s\d{2}\s\d{2}|-\d{2}-\d{2}|\d{4})|\+7\s9\d{2}\s\d{3}(?:\s\d{2}\s\d{2}|-\d{2}-\d{2}|\d{4}))$/;
const supportedPhoneHint = "+375 25 123 45 67 или +7 900 123-45-67";

function isSupportedPhone(value) {
  return supportedPhonePattern.test(String(value || "").trim());
}

function updatePhoneField(input, flag) {
  const value = input.value.trim();
  if (value.startsWith("+7") && !value.startsWith("+375")) {
    flag.textContent = "🇷🇺";
    flag.setAttribute("aria-label", "Россия");
  } else if (value.startsWith("+375") || !value) {
    flag.textContent = "🇧🇾";
    flag.setAttribute("aria-label", "Беларусь");
  } else {
    flag.textContent = "🌐";
    flag.setAttribute("aria-label", "Страна не определена");
  }
  input.setCustomValidity(value && !isSupportedPhone(value) ? "Введите телефон в формате " + supportedPhoneHint : "");
}

function enhancePhoneInput(input) {
  if (input.dataset.phoneEnhanced === "true") return;
  input.dataset.phoneEnhanced = "true";
  input.pattern = "(?:\\+375 (?:24|25|29|33|44) [0-9]{3}(?: [0-9]{2} [0-9]{2}|-[0-9]{2}-[0-9]{2}|[0-9]{4})|\\+7 9[0-9]{2} [0-9]{3}(?: [0-9]{2} [0-9]{2}|-[0-9]{2}-[0-9]{2}|[0-9]{4}))";
  input.title = supportedPhoneHint;
  input.autocomplete = "tel";

  const shell = document.createElement("span");
  shell.className = "phone-input-shell";
  const flag = document.createElement("span");
  flag.className = "phone-input-flag";
  shell.append(flag);
  input.before(shell);
  shell.append(input);
  input.addEventListener("input", () => updatePhoneField(input, flag));
  input.addEventListener("blur", () => updatePhoneField(input, flag));
  updatePhoneField(input, flag);
}

function enhancePhoneInputs(root = document) {
  if (root.matches?.('input[type="tel"]')) enhancePhoneInput(root);
  root.querySelectorAll?.('input[type="tel"]').forEach(enhancePhoneInput);
}

enhancePhoneInputs();
new MutationObserver((records) => {
  records.forEach((record) => record.addedNodes.forEach((node) => {
    if (node.nodeType === Node.ELEMENT_NODE) enhancePhoneInputs(node);
  }));
}).observe(document.body, { childList: true, subtree: true });

function showSiteToast(message, type = "success") {
  let toast = document.querySelector(".site-toast");
  if (!toast) {
    toast = document.createElement("div");
    toast.className = "site-toast";
    document.body.append(toast);
  }
  toast.innerHTML = '<span class="site-toast__icon">' + (type === "error" ? "!" : "✓") + '</span><strong>' + message + '</strong>';
  toast.classList.toggle("site-toast--error", type === "error");
  toast.classList.add("is-visible");
  clearTimeout(showSiteToast.timer);
  showSiteToast.timer = setTimeout(() => toast.classList.remove("is-visible"), 3000);
}

function createSiteModal() {
  let modal = document.querySelector("[data-site-modal]");
  if (modal) return modal;
  modal = document.createElement("div");
  modal.className = "site-modal";
  modal.dataset.siteModal = "";
  modal.setAttribute("aria-hidden", "true");
  modal.innerHTML = '<div class="site-modal__backdrop" data-site-modal-close></div><div class="site-modal__dialog"><button class="site-modal__close" type="button" data-site-modal-close aria-label="Закрыть">×</button><div data-site-modal-content></div></div>';
  document.body.append(modal);
  modal.querySelectorAll("[data-site-modal-close]").forEach((button) => button.addEventListener("click", closeSiteModal));
  return modal;
}

function clearEventEditViewportLock(modal) {
  if (!modal) return;
  const backdrop = modal.querySelector(".site-modal__backdrop");
  const dialog = modal.querySelector(".site-modal__dialog");
  [modal, backdrop, dialog].forEach((element) => {
    if (!element) return;
    ["position", "inset", "top", "right", "bottom", "left", "width", "height", "max-height", "margin", "overflow", "transform", "align-items", "justify-content"].forEach((property) => element.style.removeProperty(property));
  });
}

function lockEventEditModalToViewport(modal) {
  if (!modal?.classList.contains("site-modal--event-edit")) return;
  const backdrop = modal.querySelector(".site-modal__backdrop");
  const dialog = modal.querySelector(".site-modal__dialog");

  modal.style.setProperty("position", "fixed", "important");
  modal.style.setProperty("inset", "0", "important");
  modal.style.setProperty("width", "100vw", "important");
  modal.style.setProperty("height", "100dvh", "important");
  modal.style.setProperty("overflow", "hidden", "important");
  modal.style.setProperty("align-items", "center", "important");
  modal.style.setProperty("justify-content", "center", "important");

  backdrop?.style.setProperty("position", "absolute", "important");
  backdrop?.style.setProperty("inset", "0", "important");
  backdrop?.style.setProperty("width", "100%", "important");
  backdrop?.style.setProperty("height", "100%", "important");

  dialog?.style.setProperty("position", "relative", "important");
  dialog?.style.setProperty("top", "auto", "important");
  dialog?.style.setProperty("right", "auto", "important");
  dialog?.style.setProperty("bottom", "auto", "important");
  dialog?.style.setProperty("left", "auto", "important");
  dialog?.style.setProperty("width", "min(760px, calc(100vw - 48px))", "important");
  dialog?.style.setProperty("max-height", "calc(100dvh - 48px)", "important");
  dialog?.style.setProperty("margin", "0", "important");
  dialog?.style.setProperty("overflow", "auto", "important");
  dialog?.style.setProperty("transform", "none", "important");
}

function openSiteModal(html, modifier = "") {
  const modal = createSiteModal();
  clearEventEditViewportLock(modal);
  modal.classList.remove("site-modal--support", "site-modal--compact", "site-modal--support-ticket", "site-modal--anchored", "site-modal--event-edit");
  modal.style.removeProperty("--site-modal-top");
  modal.style.removeProperty("--site-modal-height");
  if (modifier) modal.classList.add(modifier);
  modal.querySelector("[data-site-modal-content]").innerHTML = html;
  if (typeof translateElementTree === "function") translateElementTree(localStorage.getItem(siteLanguageStorageKey) || "ru");
  modal.classList.add("is-open");
  modal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
  const dialog = modal.querySelector(".video-modal__dialog, .account-modal__dialog, .drag-register-modal__dialog, .site-modal__dialog");
  if (dialog) dialog.scrollTop = 0;
  requestAnimationFrame(() => centerVisibleModal(modal, { scrollToDialog: !modal.classList.contains("site-modal--event-edit") }));
}

function closeSiteModal() {
  const modal = document.querySelector("[data-site-modal]");
  if (!modal) return;
  clearEventEditViewportLock(modal);
  modal.classList.remove("is-open", "site-modal--support", "site-modal--compact", "site-modal--support-ticket", "site-modal--anchored", "site-modal--event-edit");
  modal.style.removeProperty("--site-modal-top");
  modal.style.removeProperty("--site-modal-height");
  modal.setAttribute("aria-hidden", "true");
  document.body.classList.remove("modal-open");
}

function getVisibleModalDialog(modal) {
  return modal?.querySelector(".video-modal__dialog, .account-modal__dialog, .drag-register-modal__dialog, .site-modal__dialog") || null;
}

function scrollPageToModal(modal) {
  const dialog = getVisibleModalDialog(modal);
  if (!dialog) return;

  requestAnimationFrame(() => {
    const rect = dialog.getBoundingClientRect();
    const targetTop = Math.max(0, window.scrollY + rect.top + (rect.height / 2) - (window.innerHeight / 2));
    window.scrollTo({ top: targetTop, behavior: "smooth" });
  });
}

function centerVisibleModal(modal, options = {}) {
  if (!modal || !modal.classList.contains("is-open")) return;
  const dialog = getVisibleModalDialog(modal);
  if (!dialog) return;
  if (modal.classList.contains("site-modal--event-edit")) {
    lockEventEditModalToViewport(modal);
    return;
  }

  modal.style.removeProperty("position");
  modal.style.removeProperty("inset");
  modal.style.removeProperty("width");
  modal.style.removeProperty("height");
  modal.style.removeProperty("overflow");
  dialog.style.removeProperty("position");
  dialog.style.removeProperty("left");
  dialog.style.removeProperty("top");
  dialog.style.removeProperty("transform");
  dialog.style.removeProperty("maxHeight");
  dialog.style.removeProperty("overflowY");

  if (options.scrollToDialog !== false) {
    scrollPageToModal(modal);
  }
}


function centerOpenModals() {
  document.querySelectorAll(".video-modal.is-open, .account-modal.is-open, .drag-register-modal.is-open, .site-modal.is-open").forEach((modal) => {
    centerVisibleModal(modal, { scrollToDialog: false });
  });
}

window.addEventListener("resize", centerOpenModals);
window.addEventListener("scroll", centerOpenModals, { passive: true });
window.visualViewport?.addEventListener("resize", centerOpenModals);
window.visualViewport?.addEventListener("scroll", centerOpenModals);

document.addEventListener("click", (event) => {
  if (event.target.closest("[data-site-modal-close]")) {
    closeSiteModal();
  }
});

document.addEventListener("submit", async (event) => {
  if (!event.target.matches("[data-support-form]")) return;
  event.preventDefault();
  const form = event.target;
  if (!form.reportValidity()) return;
  const submitButton = form.querySelector('[type="submit"]');
  const payload = {
    userId: getCurrentUser()?.id || "",
    name: form.elements.name.value.trim(),
    email: form.elements.email.value.trim().toLowerCase(),
    subject: form.elements.subject.value.trim(),
    category: form.elements.category.value.trim(),
    message: form.elements.message.value.trim()
  };

  if (submitButton) submitButton.disabled = true;
  try {
    await createSupportTicket(payload);
    form.reset();
    const message = "Вы успешно отправили обращение";
    showSiteToast(message);
  } catch (error) {
    showSiteToast(error.message || "Не удалось отправить обращение", "error");
  } finally {
    if (submitButton) submitButton.disabled = false;
  }
});

const siteLanguageStorageKey = "racemanager.siteLanguage";
const siteLanguages = {
  ru: { code: "RU", label: "Русский", flag: "🇷🇺" },
  en: { code: "EN", label: "English", flag: "🇺🇸" },
  de: { code: "DE", label: "Deutsch", flag: "🇩🇪" },
  pl: { code: "PL", label: "Polski", flag: "🇵🇱" },
};

const siteTranslations = {
  ru: { "Главная": "Главная", "Чемпионаты": "Чемпионаты", "Календарь": "Календарь", "Команды": "Команды", "Медиа": "Медиа", "Войти": "Войти", "Регистрация": "Регистрация", "Поддержка": "Поддержка", "Пользовательское соглашение": "Пользовательское соглашение", "Политика обработки персональных данных": "Политика обработки персональных данных" },
  en: { "Главная": "Home", "Чемпионаты": "Championships", "Календарь": "Calendar", "Команды": "Teams", "Медиа": "Media", "Войти": "Sign in", "Регистрация": "Registration", "Поддержка": "Support", "Пользовательское соглашение": "User agreement", "Политика обработки персональных данных": "Privacy policy" },
  de: { "Главная": "Start", "Чемпионаты": "Meisterschaften", "Календарь": "Kalender", "Команды": "Teams", "Медиа": "Medien", "Войти": "Anmelden", "Регистрация": "Anmeldung", "Поддержка": "Support", "Пользовательское соглашение": "Nutzungsvereinbarung", "Политика обработки персональных данных": "Datenschutzrichtlinie" },
  pl: { "Главная": "Główna", "Чемпионаты": "Mistrzostwa", "Календарь": "Kalendarz", "Команды": "Zespoły", "Медиа": "Media", "Войти": "Zaloguj", "Регистрация": "Rejestracja", "Поддержка": "Pomoc", "Пользовательское соглашение": "Umowa użytkownika", "Политика обработки персональных данных": "Polityka prywatności" },
};


const sitePhraseTranslations = {
  en: {
    "Официальная платформа чемпионатов": "Official championship platform",
    "Организаторам": "For organizers", "Регламент": "Regulations", "Главная": "Home", "Чемпионаты": "Championships", "Календарь": "Calendar", "Команды": "Teams", "Медиа": "Media", "Войти": "Sign in", "Профиль": "Profile",
    "Подать заявку": "Apply", "Смотреть тизер": "Watch teaser", "Подробнее": "Details", "Все чемпионаты": "All championships", "Весь календарь": "Full calendar", "Партнеры чемпионата": "Championship partners",
    "Новости": "News", "Видео": "Video", "Фотогалерея": "Photo gallery", "Участникам": "Participants", "Регистрация": "Registration", "Поддержка": "Support", "Пользовательское соглашение": "User agreement", "Политика обработки персональных данных": "Personal data policy",
    "Команды 2026 года": "2026 Teams", "Познакомьтесь с командами текущего сезона и их составами пилотов.": "Meet the teams of the current season and their driver lineups.", "Сезон 2026": "2026 season",
    "Личный кабинет": "Personal account", "Профиль пилота": "Driver profile", "Личные данные": "Personal data", "Редактировать": "Edit", "Мои автомобили": "My cars", "Основной автомобиль": "Main car", "Добавить": "Add", "Добавить автомобиль": "Add car", "Мои заявки": "My applications", "Настройки": "Settings", "Выйти из аккаунта": "Log out", "Найти событие": "Find event", "Создать событие": "Create event", "История участия": "Participation history", "Моя команда": "My team", "Позиция": "Position", "Очки": "Points", "Этапы": "Stages",
    "Гродно: тайминг сбора": "Grodno: event timing", "Конфигурация трека": "Track configuration", "Трансляции этапов": "Stage broadcasts", "Результаты пилотов": "Driver results", "Командный зачет": "Team standings",
    "Найти на RaceManager": "Search RaceManager", "Найти": "Search", "Обращение в поддержку": "Support request", "Отправить обращение": "Send request", "Имя": "Name", "Тема": "Subject", "Сообщение": "Message", "Мой профиль": "My profile", "Выйти": "Log out"
  },
  de: {
    "Официальная платформа чемпионатов": "Offizielle Meisterschaftsplattform",
    "Организаторам": "Für Veranstalter", "Регламент": "Reglement", "Главная": "Start", "Чемпионаты": "Meisterschaften", "Календарь": "Kalender", "Команды": "Teams", "Медиа": "Medien", "Войти": "Anmelden", "Профиль": "Profil",
    "Подать заявку": "Anmelden", "Смотреть тизер": "Teaser ansehen", "Подробнее": "Details", "Все чемпионаты": "Alle Meisterschaften", "Весь календарь": "Vollständiger Kalender", "Партнеры чемпионата": "Partner der Meisterschaft",
    "Новости": "Nachrichten", "Видео": "Video", "Фотогалерея": "Fotogalerie", "Участникам": "Teilnehmer", "Регистрация": "Registrierung", "Поддержка": "Support", "Пользовательское соглашение": "Nutzungsvereinbarung", "Политика обработки персональных данных": "Datenschutzrichtlinie",
    "Команды 2026 года": "Teams 2026", "Познакомьтесь с командами текущего сезона и их составами пилотов.": "Lernen Sie die Teams der aktuellen Saison und ihre Fahrer kennen.", "Сезон 2026": "Saison 2026",
    "Личный кабинет": "Persönliches Konto", "Профиль пилота": "Fahrerprofil", "Личные данные": "Persönliche Daten", "Редактировать": "Bearbeiten", "Мои автомобили": "Meine Autos", "Основной автомобиль": "Hauptfahrzeug", "Добавить": "Hinzufügen", "Добавить автомобиль": "Auto hinzufügen", "Мои заявки": "Meine Anmeldungen", "Настройки": "Einstellungen", "Выйти из аккаунта": "Abmelden", "Найти событие": "Event finden", "Создать событие": "Event erstellen", "История участия": "Teilnahmehistorie", "Моя команда": "Mein Team", "Позиция": "Position", "Очки": "Punkte", "Этапы": "Etappen",
    "Гродно: тайминг сбора": "Grodno: Zeitplan", "Конфигурация трека": "Streckenkonfiguration", "Трансляции этапов": "Etappenübertragungen", "Результаты пилотов": "Fahrergebnisse", "Командный зачет": "Teamwertung",
    "Найти на RaceManager": "RaceManager durchsuchen", "Найти": "Suchen", "Обращение в поддержку": "Supportanfrage", "Отправить обращение": "Anfrage senden", "Имя": "Name", "Тема": "Thema", "Сообщение": "Nachricht", "Мой профиль": "Mein Profil", "Выйти": "Abmelden"
  },
  pl: {
    "Официальная платформа чемпионатов": "Oficjalna platforma mistrzostw",
    "Организаторам": "Dla organizatorów", "Регламент": "Regulamin", "Главная": "Główna", "Чемпионаты": "Mistrzostwa", "Календарь": "Kalendarz", "Команды": "Zespoły", "Медиа": "Media", "Войти": "Zaloguj", "Профиль": "Profil",
    "Подать заявку": "Zgłoś się", "Смотреть тизер": "Obejrzyj teaser", "Подробнее": "Szczegóły", "Все чемпионаты": "Wszystkie mistrzostwa", "Весь календарь": "Pełny kalendarz", "Партнеры чемпионата": "Partnerzy mistrzostw",
    "Новости": "Aktualności", "Видео": "Wideo", "Фотогалерея": "Galeria", "Участникам": "Uczestnikom", "Регистрация": "Rejestracja", "Поддержка": "Pomoc", "Пользовательское соглашение": "Umowa użytkownika", "Политика обработки персональных данных": "Polityka danych osobowych",
    "Команды 2026 года": "Zespoły 2026", "Познакомьтесь с командами текущего сезона и их составами пилотов.": "Poznaj zespoły bieżącego sezonu i składy kierowców.", "Сезон 2026": "Sezon 2026",
    "Личный кабинет": "Konto osobiste", "Профиль пилота": "Profil kierowcy", "Личные данные": "Dane osobowe", "Редактировать": "Edytuj", "Мои автомобили": "Moje auta", "Основной автомобиль": "Główne auto", "Добавить": "Dodaj", "Добавить автомобиль": "Dodaj auto", "Мои заявки": "Moje zgłoszenia", "Настройки": "Ustawienia", "Выйти из аккаунта": "Wyloguj", "Найти событие": "Znajdź wydarzenie", "Создать событие": "Utwórz wydarzenie", "История участия": "Historia udziału", "Моя команда": "Mój zespół", "Позиция": "Pozycja", "Очки": "Punkty", "Этапы": "Etapy",
    "Гродно: тайминг сбора": "Grodno: harmonogram", "Конфигурация трека": "Konfiguracja toru", "Трансляции этапов": "Transmisje etapów", "Результаты пилотов": "Wyniki kierowców", "Командный зачет": "Klasyfikacja zespołów",
    "Найти на RaceManager": "Szukaj w RaceManager", "Найти": "Szukaj", "Обращение в поддержку": "Zgłoszenie do pomocy", "Отправить обращение": "Wyślij zgłoszenie", "Имя": "Imię", "Тема": "Temat", "Сообщение": "Wiadomość", "Мой профиль": "Mój profil", "Выйти": "Wyloguj"
  }
};

Object.assign(sitePhraseTranslations.en, {
  "Последние новости": "Latest news", "Все новости": "All news", "Итоги этапа": "Stage results", "Сегодня, 10:30": "Today, 10:30", "Победитель 2 этапа Betera Drift 2026 - Кирилл Мацкевич": "Winner of Betera Drift 2026 Stage 2 - Kirill Matskevich", "Пилот команды Low Budget Drift одержал победу во втором этапе сезона.": "The Low Budget Drift driver won the second stage of the season.", "Сегодня, 08:15 · Команды": "Today, 08:15 · Teams", "Антон Поздняков перешел в Betera Team": "Anton Pozdnyakov joined Betera Team", "Вице-чемпион прошлого сезона. В сезоне-2026 задача проста и амбициозна - снова оказаться на пьедестале в общем зачете.": "Last season's vice-champion. In 2026 the goal is simple and ambitious: return to the overall podium.", "Вчера, 18:40 · Drag402": "Yesterday, 18:40 · Drag402", "1 этап Drag402 от команды 22RT отменен из-за погодных условий": "Drag402 Stage 1 by 22RT cancelled due to weather", "Мы попытались сделать все возможное, но, к сожалению, погода взяла свое.": "We tried to do everything possible, but unfortunately the weather prevailed.", "Читать": "Read",
  "Медиа": "Media", "Видео от организаторов, командные истории и лучшие кадры с трассы.": "Videos from organizers, team stories and the best shots from the track.", "Смотреть хайлайты": "Watch highlights", "Фильтр контента": "Content filter", "Все": "All", "С трека": "From track", "Видео команд и организаторов": "Team and organizer videos", "Лучшие моменты этапа: скорость, дым и парные заезды": "Best stage moments: speed, smoke and tandem runs", "Главные эпизоды гоночного уикенда в одном видео.": "Main moments of the race weekend in one video.", "Дрифт Стайки глазами зрителей": "Stayki drift through spectators' eyes", "Дрифт в Беларуси: как это проходит": "Drift in Belarus: how it works", "Командное видео из паддока": "Team video from the paddock", "Атмосфера гоночного уикенда": "Race weekend atmosphere", "Подготовка автомобилей к старту": "Preparing cars for the start", "Яркие кадры с трассы": "Bright shots from the track", "Командные фотографии": "Team photos", "Лучшие кадры с трека": "Best shots from the track", "6 фотографий": "6 photos", "Команда, пилоты и атмосфера сезона": "Team, drivers and season atmosphere", "Красная линия и боевые заезды": "Red line and battle runs", "Технологичный стиль команды": "High-tech team style", "Лидеры командного зачета": "Team standings leaders", "В зоне постановки": "In the initiation zone", "Точный заезд": "Precise run", "Красная атака": "Red attack", "В движении": "In motion", "Эмоции этапа": "Stage emotions", "Подготовка к старту": "Preparing for start", "Новые кадры с трассы": "New shots from the track", "В ожидании старта": "Waiting for start", "Работа в паддоке": "Work in the paddock", "Эмоции гоночного дня": "Race day emotions"
});
Object.assign(sitePhraseTranslations.de, {
  "Последние новости": "Neueste Nachrichten", "Все новости": "Alle Nachrichten", "Итоги этапа": "Etappenergebnisse", "Сегодня, 10:30": "Heute, 10:30", "Победитель 2 этапа Betera Drift 2026 - Кирилл Мацкевич": "Sieger der 2. Etappe Betera Drift 2026 - Kirill Matskevich", "Пилот команды Low Budget Drift одержал победу во втором этапе сезона.": "Der Fahrer von Low Budget Drift gewann die zweite Etappe der Saison.", "Сегодня, 08:15 · Команды": "Heute, 08:15 · Teams", "Антон Поздняков перешел в Betera Team": "Anton Pozdnyakov wechselt zu Betera Team", "Вице-чемпион прошлого сезона. В сезоне-2026 задача проста и амбициозна - снова оказаться на пьедестале в общем зачете.": "Der Vizemeister der letzten Saison. 2026 ist das Ziel klar und ehrgeizig: wieder aufs Gesamtpodium.", "Вчера, 18:40 · Drag402": "Gestern, 18:40 · Drag402", "1 этап Drag402 от команды 22RT отменен из-за погодных условий": "Drag402 Etappe 1 von 22RT wegen Wetters abgesagt", "Мы попытались сделать все возможное, но, к сожалению, погода взяла свое.": "Wir haben alles versucht, aber leider setzte sich das Wetter durch.", "Читать": "Lesen",
  "Видео от организаторов, командные истории и лучшие кадры с трассы.": "Videos von Veranstaltern, Teamgeschichten und die besten Streckenbilder.", "Смотреть хайлайты": "Highlights ansehen", "Фильтр контента": "Inhaltsfilter", "Все": "Alle", "С трека": "Von der Strecke", "Видео команд и организаторов": "Videos von Teams und Veranstaltern", "Лучшие моменты этапа: скорость, дым и парные заезды": "Beste Momente der Etappe: Geschwindigkeit, Rauch und Tandemläufe", "Главные эпизоды гоночного уикенда в одном видео.": "Die wichtigsten Momente des Rennwochenendes in einem Video.", "Дрифт Стайки глазами зрителей": "Drift in Stayki aus Zuschauersicht", "Дрифт в Беларуси: как это проходит": "Drift in Belarus: so läuft es", "Командное видео из паддока": "Teamvideo aus dem Fahrerlager", "Атмосфера гоночного уикенда": "Atmosphäre des Rennwochenendes", "Подготовка автомобилей к старту": "Fahrzeuge für den Start vorbereiten", "Яркие кадры с трассы": "Starke Bilder von der Strecke", "Командные фотографии": "Teamfotos", "Лучшие кадры с трека": "Beste Streckenbilder", "6 фотографий": "6 Fotos", "Команда, пилоты и атмосфера сезона": "Team, Fahrer und Saisonatmosphäre", "Красная линия и боевые заезды": "Rote Linie und Kampf-Läufe", "Технологичный стиль команды": "Technologischer Teamstil", "Лидеры командного зачета": "Führende der Teamwertung", "В зоне постановки": "In der Einlenkzone", "Точный заезд": "Präziser Lauf", "Красная атака": "Rote Attacke", "В движении": "In Bewegung", "Эмоции этапа": "Emotionen der Etappe", "Подготовка к старту": "Startvorbereitung", "Новые кадры с трассы": "Neue Bilder von der Strecke", "В ожидании старта": "Warten auf den Start", "Работа в паддоке": "Arbeit im Fahrerlager", "Эмоции гоночного дня": "Emotionen des Renntags"
});
Object.assign(sitePhraseTranslations.pl, {
  "Последние новости": "Najnowsze wiadomości", "Все новости": "Wszystkie wiadomości", "Итоги этапа": "Wyniki etapu", "Сегодня, 10:30": "Dzisiaj, 10:30", "Победитель 2 этапа Betera Drift 2026 - Кирилл Мацкевич": "Zwycięzca 2 etapu Betera Drift 2026 - Kirył Mackiewicz", "Пилот команды Low Budget Drift одержал победу во втором этапе сезона.": "Kierowca Low Budget Drift wygrał drugi etap sezonu.", "Сегодня, 08:15 · Команды": "Dzisiaj, 08:15 · Zespoły", "Антон Поздняков перешел в Betera Team": "Anton Pozdniakow przeszedł do Betera Team", "Вице-чемпион прошлого сезона. В сезоне-2026 задача проста и амбициозна - снова оказаться на пьедестале в общем зачете.": "Wicemistrz poprzedniego sezonu. W sezonie 2026 cel jest prosty i ambitny: wrócić na podium klasyfikacji generalnej.", "Вчера, 18:40 · Drag402": "Wczoraj, 18:40 · Drag402", "1 этап Drag402 от команды 22RT отменен из-за погодных условий": "1 etap Drag402 zespołu 22RT odwołany z powodu pogody", "Мы попытались сделать все возможное, но, к сожалению, погода взяла свое.": "Próbowaliśmy zrobić wszystko, ale niestety pogoda wygrała.", "Читать": "Czytaj",
  "Видео от организаторов, командные истории и лучшие кадры с трассы.": "Filmy od organizatorów, historie zespołów i najlepsze ujęcia z toru.", "Смотреть хайлайты": "Zobacz skrót", "Фильтр контента": "Filtr treści", "Все": "Wszystko", "С трека": "Z toru", "Видео команд и организаторов": "Filmy zespołów i organizatorów", "Лучшие моменты этапа: скорость, дым и парные заезды": "Najlepsze momenty etapu: prędkość, dym i przejazdy w parach", "Главные эпизоды гоночного уикенда в одном видео.": "Najważniejsze momenty weekendu wyścigowego w jednym filmie.", "Дрифт Стайки глазами зрителей": "Drift w Stajkach oczami widzów", "Дрифт в Беларуси: как это проходит": "Drift na Białorusi: jak to wygląda", "Командное видео из паддока": "Film zespołu z paddocku", "Атмосфера гоночного уикенда": "Atmosfera weekendu wyścigowego", "Подготовка автомобилей к старту": "Przygotowanie samochodów do startu", "Яркие кадры с трассы": "Najlepsze ujęcia z toru", "Командные фотографии": "Zdjęcia zespołów", "Лучшие кадры с трека": "Najlepsze kadry z toru", "6 фотографий": "6 zdjęć", "Команда, пилоты и атмосфера сезона": "Zespół, kierowcy i atmosfera sezonu", "Красная линия и боевые заезды": "Czerwona linia i bojowe przejazdy", "Технологичный стиль команды": "Technologiczny styl zespołu", "Лидеры командного зачета": "Liderzy klasyfikacji zespołowej", "В зоне постановки": "W strefie inicjacji", "Точный заезд": "Precyzyjny przejazd", "Красная атака": "Czerwony atak", "В движении": "W ruchu", "Эмоции этапа": "Emocje etapu", "Подготовка к старту": "Przygotowanie do startu", "Новые кадры с трассы": "Nowe kadry z toru", "В ожидании старта": "W oczekiwaniu na start", "Работа в паддоке": "Praca w paddocku", "Эмоции гоночного дня": "Emocje dnia wyścigowego"
});

const partialTranslations = {
  en: [["Добро пожаловать", "Welcome"], ["Здесь собрана основная информация для участия в соревнованиях.", "Key information for race participation is collected here."], ["Пилот", "Driver"], ["Гонки", "Races"], ["Победы", "Wins"], ["Подиумы", "Podiums"], ["Лучший результат", "Best result"], ["Текущий сезон", "Current season"], ["Общий рейтинг участников", "Overall participant ranking"], ["Первые места", "First places"], ["Финиши в топ-3", "Top-3 finishes"], ["Лучший финиш", "Best finish"], ["Открыта регистрация", "Registration open"], ["Ближайшие события", "Upcoming events"], ["Все чемпионаты", "All championships"], ["О команде", "About the team"], ["Результаты команды", "Team results"], ["Галерея команды", "Team gallery"], ["Пилоты", "Drivers"], ["Добавляйте автомобили", "Add cars"], ["Автомобиль не указан", "Car not specified"], ["очков", "points"], ["очка", "points"], ["очко", "point"], ["После 2 этапов", "After 2 stages"], ["Личный зачет", "Driver standings"], ["Командный зачёт", "Team standings"], ["Платформа автоспортивных чемпионатов", "Motorsport championship platform"]],
  de: [["Добро пожаловать", "Willkommen"], ["Здесь собрана основная информация для участия в соревнованиях.", "Hier finden Sie die wichtigsten Informationen zur Teilnahme."], ["Пилот", "Fahrer"], ["Гонки", "Rennen"], ["Победы", "Siege"], ["Подиумы", "Podien"], ["Лучший результат", "Bestes Ergebnis"], ["Текущий сезон", "Aktuelle Saison"], ["Общий рейтинг участников", "Gesamtrangliste der Teilnehmer"], ["Первые места", "Erste Plätze"], ["Финиши в топ-3", "Top-3-Platzierungen"], ["Лучший финиш", "Bestes Finish"], ["Открыта регистрация", "Registrierung offen"], ["Ближайшие события", "Kommende Events"], ["Все чемпионаты", "Alle Meisterschaften"], ["О команде", "Über das Team"], ["Результаты команды", "Teamergebnisse"], ["Галерея команды", "Teamgalerie"], ["Пилоты", "Fahrer"], ["Добавляйте автомобили", "Fahrzeuge hinzufügen"], ["Автомобиль не указан", "Fahrzeug nicht angegeben"], ["очков", "Punkte"], ["очка", "Punkte"], ["очко", "Punkt"], ["После 2 этапов", "Nach 2 Etappen"], ["Личный зачет", "Fahrerwertung"], ["Командный зачёт", "Teamwertung"], ["Платформа автоспортивных чемпионатов", "Motorsport-Meisterschaftsplattform"]],
  pl: [["Добро пожаловать", "Witamy"], ["Здесь собрана основная информация для участия в соревнованиях.", "Tutaj zebrano podstawowe informacje do udziału w zawodach."], ["Пилот", "Kierowca"], ["Гонки", "Wyścigi"], ["Победы", "Zwycięstwa"], ["Подиумы", "Podia"], ["Лучший результат", "Najlepszy wynik"], ["Текущий сезон", "Bieżący sezon"], ["Общий рейтинг участников", "Ogólny ranking uczestników"], ["Первые места", "Pierwsze miejsca"], ["Финиши в топ-3", "Miejsca w top-3"], ["Лучший финиш", "Najlepszy finisz"], ["Открыта регистрация", "Rejestracja otwarta"], ["Ближайшие события", "Nadchodzące wydarzenia"], ["Все чемпионаты", "Wszystkie mistrzostwa"], ["О команде", "O zespole"], ["Результаты команды", "Wyniki zespołu"], ["Галерея команды", "Galeria zespołu"], ["Пилоты", "Kierowcy"], ["Добавляйте автомобили", "Dodawaj auta"], ["Автомобиль не указан", "Auto nie wskazane"], ["очков", "punktów"], ["очка", "punkty"], ["очко", "punkt"], ["После 2 этапов", "Po 2 etapach"], ["Личный зачет", "Klasyfikacja kierowców"], ["Командный зачёт", "Klasyfikacja zespołów"], ["Платформа автоспортивных чемпионатов", "Platforma mistrzostw motorsportowych"]]
};

function translateSiteText(original, lang) {
  if (lang === "ru") return original;
  const trimmed = original.trim();
  const leading = original.match(/^\s*/)?.[0] || "";
  const trailing = original.match(/\s*$/)?.[0] || "";
  const dictionary = sitePhraseTranslations[lang] || {};
  let translated = dictionary[trimmed];
  if (!translated) {
    translated = trimmed;
    for (const [from, to] of partialTranslations[lang] || []) translated = translated.replaceAll(from, to);
    if (translated === trimmed) return original;
  }
  return leading + translated + trailing;
}

function translateElementTree(lang) {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
    acceptNode(node) {
      const parent = node.parentElement;
      if (!parent || ["SCRIPT", "STYLE", "NOSCRIPT", "TEXTAREA"].includes(parent.tagName)) return NodeFilter.FILTER_REJECT;
      if (!node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
      return NodeFilter.FILTER_ACCEPT;
    }
  });
  const nodes = [];
  while (walker.nextNode()) nodes.push(walker.currentNode);
  nodes.forEach((node) => {
    node.__raceManagerOriginalText ||= node.nodeValue;
    node.nodeValue = translateSiteText(node.__raceManagerOriginalText, lang);
  });
  document.querySelectorAll("input[placeholder], textarea[placeholder]").forEach((element) => {
    element.dataset.i18nPlaceholderOriginal ||= element.placeholder;
    element.placeholder = translateSiteText(element.dataset.i18nPlaceholderOriginal, lang);
  });
}

function setupSiteLanguage() {
  const triggers = document.querySelectorAll("[data-language-trigger]");
  if (!triggers.length) return;
  let current = localStorage.getItem(siteLanguageStorageKey) || "ru";
  const applyLanguage = (lang) => {
    current = siteLanguages[lang] ? lang : "ru";
    localStorage.setItem(siteLanguageStorageKey, current);
    document.querySelectorAll("[data-current-language]").forEach((el) => { el.textContent = siteLanguages[current].code; });
    translateElementTree(current);
  };
  triggers.forEach((trigger) => {
    if (trigger.nextElementSibling?.classList.contains("language-menu")) return;
    const panel = document.createElement("div");
    panel.className = "language-menu";
    panel.innerHTML = Object.entries(siteLanguages).map(([key, lang]) => '<button type="button" data-site-lang="' + key + '"><span>' + lang.flag + '</span>' + lang.label + '</button>').join("");
    trigger.after(panel);
    trigger.addEventListener("click", () => panel.classList.toggle("is-open"));
    panel.addEventListener("click", (event) => {
      const button = event.target.closest("[data-site-lang]");
      if (!button) return;
      applyLanguage(button.dataset.siteLang);
      panel.classList.remove("is-open");
    });
  });
  applyLanguage(current);
}

setupSiteLanguage();

function initCookieBanner() {
  const storageKey = "racemanager.cookiesAccepted";
  if (localStorage.getItem(storageKey) === "true" || document.querySelector("[data-cookie-banner]")) return;
  const banner = document.createElement("section");
  banner.className = "cookie-banner";
  banner.dataset.cookieBanner = "";
  banner.setAttribute("aria-label", "Уведомление о cookie");
  banner.innerHTML = '<div class="cookie-banner__icon">R</div><div class="cookie-banner__copy"><strong>Мы используем cookie</strong><p>RaceManager сохраняет технические cookie для авторизации, заявок, языка интерфейса и корректной работы личного кабинета.</p></div><div class="cookie-banner__actions"><button class="cookie-banner__secondary" type="button" data-cookie-more>Подробнее</button><button class="cookie-banner__accept" type="button" data-cookie-accept>Принять</button></div>';
  document.body.append(banner);
  requestAnimationFrame(() => banner.classList.add("is-visible"));
  banner.querySelector("[data-cookie-accept]").addEventListener("click", () => {
    localStorage.setItem(storageKey, "true");
    banner.classList.remove("is-visible");
    setTimeout(() => banner.remove(), 250);
  });
  banner.querySelector("[data-cookie-more]").addEventListener("click", () => {
    openSiteModal('<div class="site-modal__head"><span class="eyebrow eyebrow--red">Cookie</span><h2>Зачем нужны cookie</h2><p>Мы используем cookie и localStorage, чтобы запоминать вход в аккаунт, выбранный язык, принятые уведомления, заявки на события и настройки профиля. Данные нужны для работы прототипа RaceManager и не используются для рекламного трекинга.</p></div><div class="confirm-actions"><button class="button button--red" type="button" data-site-modal-close>Понятно</button></div>');
  });
}

initCookieBanner();

const searchPages = [
  { title: "Главная", url: "/index.html", keywords: "новости таблица пилоты старт" },
  { title: "Чемпионаты", url: "/championships.html", keywords: "betera gorilla drag402 uracing чемпионат" },
  { title: "Календарь", url: "/calendar.html", keywords: "регистрация этап тренировка календарь" },
  { title: "Команды", url: "/teams.html", keywords: "betera drift racing park blockchain low budget пилоты" },
  { title: "Медиа", url: "/media.html", keywords: "видео фото галерея хайлайты" },
  { title: "Личный кабинет", url: "/pages/account/account.html", keywords: "профиль заявки избранное автомобили настройки" },
];

function ensureGlobalHeaderSearchTriggers() {
  document.querySelectorAll(".site-header .header-actions").forEach((actions) => {
    if (actions.querySelector("[data-search-open]")) return;
    const button = document.createElement("button");
    button.className = "icon-button";
    button.type = "button";
    button.dataset.searchOpen = "";
    button.setAttribute("aria-label", "Поиск");
    button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="6"></circle><path d="m16 16 5 5"></path></svg>';
    actions.prepend(button);
  });

  document.querySelectorAll("[data-api-team-filter-form], [data-calendar-api-filter]").forEach((element) => element.remove());
}

ensureGlobalHeaderSearchTriggers();

function closeHeaderSearchMenus(except = null) {
  document.querySelectorAll("[data-header-search-menu]").forEach((menu) => {
    if (menu !== except) menu.classList.remove("is-open");
  });
}

function ensureHeaderSearchMenu(trigger) {
  const actions = trigger.closest(".header-actions") || trigger.parentElement;
  if (!actions) return null;
  let wrap = trigger.closest("[data-header-search-menu]");
  if (wrap) return wrap;
  wrap = document.createElement("div");
  wrap.className = "header-search-menu";
  wrap.dataset.headerSearchMenu = "";
  trigger.before(wrap);
  wrap.append(trigger);
  wrap.insertAdjacentHTML("beforeend", '<form class="site-search-panel header-search-dropdown" data-site-search-form><div class="site-search-panel__box"><input type="search" name="query" placeholder="Поиск..." autocomplete="off"><div class="site-search-panel__actions site-search-panel__actions--single"><button class="site-search-panel__submit" type="submit"><span>⌕</span> Поиск</button></div></div><div class="site-search__results" data-site-search-results></div></form>');
  return wrap;
}

document.addEventListener("click", (event) => {
  const trigger = event.target.closest("[data-search-open]");
  const searchMenu = event.target.closest("[data-header-search-menu]");
  if (trigger) {
    event.preventDefault();
    const menu = ensureHeaderSearchMenu(trigger);
    if (!menu) return;
    const isOpen = menu.classList.toggle("is-open");
    closeHeaderSearchMenus(menu);
    if (isOpen) menu.querySelector("input")?.focus();
    return;
  }
  if (!searchMenu) closeHeaderSearchMenus();
});

async function searchSiteContent(query) {
  const normalizedQuery = query.toLowerCase();
  const matches = searchPages
    .filter((page) => `${page.title} ${page.keywords}`.toLowerCase().includes(normalizedQuery))
    .map((page) => ({ title: page.title, details: page.keywords, url: page.url }));

  const params = new URLSearchParams({ q: query, sort: "date" });
  const [eventsResult, teamsResult] = await Promise.allSettled([
    fetch(`/api/events?${params}`, { cache: "no-store" }),
    fetch(`/api/users/team-catalog?q=${encodeURIComponent(query)}`, { cache: "no-store" })
  ]);

  if (eventsResult.status === "fulfilled" && eventsResult.value.ok) {
    const events = await eventsResult.value.json();
    (Array.isArray(events) ? events : []).slice(0, 8).forEach((raceEvent) => {
      matches.push({
        title: raceEvent.title || raceEvent.name || "Событие RaceManager",
        details: [raceEvent.type, raceEvent.discipline, raceEvent.track].filter(Boolean).join(" · "),
        url: apiEventHref(raceEvent)
      });
    });
  }

  if (teamsResult.status === "fulfilled" && teamsResult.value.ok) {
    const teams = await teamsResult.value.json();
    (Array.isArray(teams) ? teams : []).slice(0, 8).forEach((team) => {
      const profile = team.profile || {};
      [
        [profile.organizationName, "Команда организаторов"],
        [profile.racingTeamName, "Гоночная команда"]
      ].forEach(([name, type]) => {
        if (name) matches.push({ title: name, details: type, url: "/teams.html" });
      });
    });
  }

  const unique = new Map();
  matches.forEach((item) => unique.set(`${item.url}|${item.title}`, item));
  return [...unique.values()].slice(0, 16);
}

document.addEventListener("submit", async (event) => {
  if (!event.target.matches("[data-site-search-form]")) return;
  event.preventDefault();
  const form = event.target;
  const query = (form.elements.query?.value || "").trim();
  const results = form.querySelector("[data-site-search-results]");
  if (!results) return;
  if (!query) {
    results.innerHTML = "<p>Введите запрос для поиска.</p>";
    return;
  }

  results.innerHTML = "<p>Ищем страницы, события и команды...</p>";
  try {
    const matches = await searchSiteContent(query);
    results.innerHTML = matches.length
      ? matches.map((item) => `<a href="${escapeHtml(item.url)}"><strong>${escapeHtml(item.title)}</strong><span>${escapeHtml(item.details || "RaceManager")}</span></a>`).join("")
      : "<p>Ничего не найдено. Попробуйте другой запрос.</p>";
  } catch (error) {
    results.innerHTML = "<p>Поиск временно недоступен.</p>";
  }
});


const teamsMega = document.querySelector(".teams-mega");
const teamsNavLink = document.querySelector('.main-nav a[href="/teams.html"], .main-nav a[href="teams.html"]');
if (teamsMega && teamsNavLink) {
  let teamsMegaTimer;
  const openTeamsMega = () => {
    clearTimeout(teamsMegaTimer);
    teamsMega.classList.add("is-open");
  };
  const closeTeamsMega = () => {
    teamsMegaTimer = setTimeout(() => teamsMega.classList.remove("is-open"), 120);
  };
  teamsNavLink.addEventListener("mouseenter", openTeamsMega);
  teamsNavLink.addEventListener("focus", openTeamsMega);
  teamsNavLink.addEventListener("mouseleave", closeTeamsMega);
  teamsNavLink.addEventListener("blur", closeTeamsMega);
  teamsMega.addEventListener("mouseenter", openTeamsMega);
  teamsMega.addEventListener("mouseleave", closeTeamsMega);
}

const countdownCard = document.querySelector("[data-race-date]");

function updateCountdown() {
  if (!countdownCard) return;

  const raceDate = new Date(countdownCard.dataset.raceDate).getTime();
  const distance = Math.max(0, raceDate - Date.now());
  const values = {
    days: Math.floor(distance / 86400000),
    hours: Math.floor((distance / 3600000) % 24),
    minutes: Math.floor((distance / 60000) % 60),
    seconds: Math.floor((distance / 1000) % 60),
  };

  Object.entries(values).forEach(([name, value]) => {
    const field = countdownCard.querySelector(`[data-countdown="${name}"]`);
    if (field) field.textContent = String(value).padStart(2, "0");
  });
}

updateCountdown();
setInterval(updateCountdown, 1000);

const videoModal = document.querySelector(".video-modal");
const videoFrame = document.querySelector(".video-modal__frame");
const videoTriggers = document.querySelectorAll("[data-video-id]");
let activeVideoTrigger = null;

function closeVideoModal() {
  if (!videoModal || !videoFrame) return;

  videoModal.classList.remove("is-open", "video-modal--anchored");
  videoModal.style.removeProperty("--video-modal-top");
  videoModal.style.removeProperty("--video-modal-height");
  videoModal.setAttribute("aria-hidden", "true");
  videoFrame.replaceChildren();
  document.body.classList.remove("modal-open");
  activeVideoTrigger?.focus();
  activeVideoTrigger = null;
}

function openVideoModal(trigger) {
  if (!videoModal || !videoFrame) return;

  activeVideoTrigger = trigger;
  const iframe = document.createElement("iframe");
  iframe.src = `https://www.youtube.com/embed/${trigger.dataset.videoId}?autoplay=1&rel=0`;
  iframe.title = trigger.dataset.videoTitle || "Видео RaceManager";
  iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";
  iframe.allowFullscreen = true;
  videoFrame.replaceChildren(iframe);
  videoModal.classList.add("is-open");
  videoModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
  videoModal.scrollTop = 0;
  const dialog = videoModal.querySelector(".video-modal__dialog");
  if (dialog) dialog.scrollTop = 0;
  requestAnimationFrame(() => centerVisibleModal(videoModal));
}

videoTriggers.forEach((trigger) => {
  trigger.addEventListener("click", (event) => {
    event.preventDefault();
    openVideoModal(trigger);
  });
});

document.querySelectorAll("[data-video-close]").forEach((button) => {
  button.addEventListener("click", closeVideoModal);
});


document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && videoModal?.classList.contains("is-open")) {
    closeVideoModal();
  }
  if (event.key === "Escape" && document.querySelector("[data-site-modal].is-open")) {
    closeSiteModal();
  }
});

const disciplineFilters = document.querySelectorAll("[data-discipline-filter]");
const championshipCards = document.querySelectorAll("[data-discipline]");
const championshipEmpty = document.querySelector(".championship-empty");

disciplineFilters.forEach((filter) => {
  filter.addEventListener("click", () => {
    const selectedDiscipline = filter.dataset.disciplineFilter;
    let visibleCards = 0;

    disciplineFilters.forEach((button) => {
      const isActive = button === filter;
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", String(isActive));
    });

    championshipCards.forEach((card) => {
      const isVisible = selectedDiscipline === "all" || card.dataset.discipline === selectedDiscipline;
      card.hidden = !isVisible;
      if (isVisible) visibleCards += 1;
    });

    if (championshipEmpty) championshipEmpty.hidden = visibleCards > 0;
  });
});


function apiDisciplineValue(key) {
  return key === "drag" ? "Дрэг" : (key === "circuit" ? "Тайм" : (key === "drift" ? "Дрифт" : ""));
}

function apiEventHref(event) {
  return `/pages/account/CreatedEvent.html?id=${encodeURIComponent(event.id)}`;
}

function apiEventCardHtml(event, compact = false) {
  const image = event.calendarBannerImage || event.bannerImage || "/public/drag402banner.jpg";
  const color = event.organizerColor || "#e10600";
  const title = escapeHtml(event.title || event.name || "Событие RaceManager");
  const meta = escapeHtml([event.track, event.discipline].filter(Boolean).join(" · ") || "RaceManager");
  const date = escapeHtml(formatEventDate(event.date));
  if (compact) {
    return `<article class="api-event-card api-event-card--compact"><img src="${escapeHtml(image)}" alt="${title}"><div><span>${escapeHtml(event.type || "Событие")}</span><h3>${title}</h3><p>${meta}</p><time>${date}</time></div><a class="button button--red button--small" style="background:${escapeHtml(color)}" href="${apiEventHref(event)}">Открыть</a></article>`;
  }
  return `<article class="championship-card championship-card--api" data-api-championship-card style="--api-color:${escapeHtml(color)}"><div class="championship-card__top"><span class="championship-card__type">${escapeHtml(event.discipline || "Дисциплина")}</span><span class="championship-card__status"><i></i> ${escapeHtml(event.registrationStatus || "Статус")}</span></div><div class="championship-card__brand"><span>${escapeHtml(event.organizerName || "RaceManager")}</span><strong>${title}</strong><small>${escapeHtml(event.type || "Событие")}</small></div><div class="championship-card__info"><div><b>${date}</b><span>дата</span></div><div><b>${escapeHtml(event.track || "—")}</b><span>трасса</span></div></div><div class="championship-card__footer"><p>${escapeHtml(event.participantLimit || 0)} участников · ${escapeHtml(event.discipline || "")}</p><a href="${apiEventHref(event)}">Открыть <span>→</span></a></div></article>`;
}

async function loadChampionshipsViaApi(discipline = "all") {
  const grid = document.querySelector(".championship-grid");
  if (!grid) return;
  grid.querySelectorAll("[data-api-championship-card]").forEach((card) => card.remove());
  const params = new URLSearchParams({ type: "Чемпионат", sort: "date" });
  const apiDiscipline = apiDisciplineValue(discipline);
  if (apiDiscipline) params.set("discipline", apiDiscipline);
  const response = await fetch(`/api/events?${params}`, { cache: "no-store" });
  if (!response.ok) return;
  const events = await response.json();
  (Array.isArray(events) ? events : []).forEach((event) => {
    const wrap = document.createElement("div");
    wrap.innerHTML = apiEventCardHtml(event);
    grid.append(...wrap.children);
  });
}

disciplineFilters.forEach((filter) => {
  filter.addEventListener("click", () => {
    loadChampionshipsViaApi(filter.dataset.disciplineFilter).catch(() => {});
  });
});
if (disciplineFilters.length) loadChampionshipsViaApi("all").catch(() => {});


function normalizeCalendarFilterValue(value = "") {
  return String(value)
    .toLowerCase()
    .replaceAll("ё", "е")
    .replace(/[–—-]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function calendarDisciplineKey(value = "") {
  const normalized = normalizeCalendarFilterValue(value);
  if (normalized.includes("дрэг") || normalized.includes("drag")) return "drag";
  if (normalized.includes("тайм") || normalized.includes("time") || normalized.includes("кольц")) return "timeattack";
  if (normalized.includes("дрифт") || normalized.includes("drift")) return "drift";
  return "";
}

function staticCalendarEventData(card) {
  const block = card.closest(".calendar-series-block");
  const blockText = block?.querySelector(".calendar-series-cover")?.textContent || "";
  const text = normalizeCalendarFilterValue(`${card.textContent || ""} ${blockText}`);
  const discipline = card.dataset.calendarDiscipline ||
    (block?.classList.contains("calendar-series-block--drag402") ? "Дрэг" :
      (block?.classList.contains("calendar-series-block--betera") ? "Дрифт" : text));
  return {
    title: card.querySelector("h3")?.textContent.trim() || "",
    type: card.dataset.calendarType || (block?.classList.contains("calendar-series-block--created") ? text : "Чемпионат"),
    discipline,
    date: card.dataset.calendarDate || card.querySelector("time")?.getAttribute("datetime") || "",
    text
  };
}

function filterStaticCalendarEvents(formData) {
  const query = normalizeCalendarFilterValue(formData.get("q"));
  const selectedType = normalizeCalendarFilterValue(formData.get("type"));
  const selectedDiscipline = calendarDisciplineKey(formData.get("discipline"));
  const sort = String(formData.get("sort") || "date");
  let visibleCount = 0;

  document.querySelectorAll(".calendar-series-list").forEach((list) => {
    const cards = [...list.querySelectorAll(":scope > .calendar-series-event")];
    cards.sort((left, right) => {
      const leftData = staticCalendarEventData(left);
      const rightData = staticCalendarEventData(right);
      if (sort === "title") return leftData.title.localeCompare(rightData.title, "ru");
      if (sort === "discipline") return calendarDisciplineKey(leftData.discipline).localeCompare(calendarDisciplineKey(rightData.discipline));
      return String(leftData.date || "9999-12-31").localeCompare(String(rightData.date || "9999-12-31"));
    }).forEach((card) => list.append(card));
  });

  document.querySelectorAll(".calendar-series-event").forEach((card) => {
    const data = staticCalendarEventData(card);
    const typeMatches = !selectedType || normalizeCalendarFilterValue(data.type).includes(selectedType);
    const disciplineMatches = !selectedDiscipline || calendarDisciplineKey(data.discipline) === selectedDiscipline;
    const queryMatches = !query || data.text.includes(query);
    const visible = typeMatches && disciplineMatches && queryMatches;
    card.hidden = !visible;
    if (visible) visibleCount += 1;
  });

  document.querySelectorAll(".calendar-series-block").forEach((block) => {
    block.hidden = !block.querySelector(".calendar-series-event:not([hidden])");
  });
  return visibleCount;
}

async function applyCalendarApiFilters(form) {
  const results = document.querySelector("[data-api-event-filter-results]");
  if (!form || !results) return;
  const formData = new FormData(form);
  const staticCount = filterStaticCalendarEvents(formData);
  const params = new URLSearchParams();
  ["q", "type", "discipline", "sort"].forEach((key) => {
    const value = String(formData.get(key) || "").trim();
    if (value) params.set(key, value);
  });

  const response = await fetch(`/api/events?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Не удалось загрузить события через API.");
  const events = await response.json();
  const apiEvents = Array.isArray(events) ? events : [];
  results.hidden = apiEvents.length === 0 && staticCount > 0;
  results.innerHTML = apiEvents.length
    ? apiEvents.map((event) => apiEventCardHtml(event, true)).join("")
    : (staticCount === 0 ? '<p class="api-filter-empty">По выбранным фильтрам события не найдены.</p>' : "");
}

const calendarApiFilterForm = document.querySelector("[data-api-event-filter-form]");
let calendarFilterTimer = 0;
calendarApiFilterForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  try { await applyCalendarApiFilters(event.currentTarget); }
  catch (error) { showSiteToast(error.message || "Фильтр API недоступен", "error"); }
});
calendarApiFilterForm?.addEventListener("change", () => applyCalendarApiFilters(calendarApiFilterForm).catch((error) => showSiteToast(error.message, "error")));
calendarApiFilterForm?.addEventListener("input", () => {
  clearTimeout(calendarFilterTimer);
  calendarFilterTimer = setTimeout(() => applyCalendarApiFilters(calendarApiFilterForm).catch(() => {}), 250);
});
calendarApiFilterForm?.addEventListener("reset", () => {
  requestAnimationFrame(() => applyCalendarApiFilters(calendarApiFilterForm).catch(() => {}));
});
if (calendarApiFilterForm) applyCalendarApiFilters(calendarApiFilterForm).catch(() => {});

function teamCatalogCardHtml(team, type) {
  const profile = team.profile || {};
  const name = type === "organization" ? profile.organizationName : profile.racingTeamName;
  const color = type === "organization" ? profile.organizationColor : profile.racingTeamColor;
  const logo = type === "organization" ? profile.organizationLogo : profile.racingTeamLogo;
  const banner = type === "organization" ? profile.organizationBanner : profile.racingTeamBanner;
  const members = type === "organization" ? profile.organizationMembers : profile.racingTeamMembers;
  if (!name) return "";
  return `<article class="teams-catalog-card teams-catalog-card--api" data-api-team-card style="--team-color:${escapeHtml(color || "#e10600")};--team-banner:url('${escapeHtml(banner || "/public/drag402banner.jpg")}')"><span class="teams-catalog-card__type">${type === "organization" ? "Команда организаторов" : "Гоночная команда"}</span><h2>${escapeHtml(name)}</h2>${logo ? `<span class="teams-catalog-card__logo"><img src="${escapeHtml(logo)}" alt="${escapeHtml(name)}"></span>` : ""}<div class="teams-catalog-card__drivers">${(members || []).slice(0, 4).map((member) => `<span>${escapeHtml(member.fullName || member.email || "Участник")}</span>`).join("")}</div></article>`;
}

async function loadTeamCatalogViaApi(form) {
  const grid = document.querySelector("[data-racing-team-catalog]");
  if (!grid) return;
  grid.querySelectorAll("[data-api-team-card]").forEach((card) => card.remove());
  const q = form ? String(new FormData(form).get("q") || "").trim() : "";
  const response = await fetch(`/api/users/team-catalog${q ? `?q=${encodeURIComponent(q)}` : ""}`, { cache: "no-store" });
  if (!response.ok) return;
  const teams = await response.json();
  const html = (Array.isArray(teams) ? teams : []).flatMap((team) => [teamCatalogCardHtml(team, "organization"), teamCatalogCardHtml(team, "racing")]).filter(Boolean).join("");
  if (html) grid.insertAdjacentHTML("afterbegin", html);
}

if (document.querySelector("[data-racing-team-catalog]")) {
  loadTeamCatalogViaApi(null).catch(() => {});
}

const timezoneLabel = document.querySelector("[data-timezone]");
const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
if (timezoneLabel) timezoneLabel.textContent = timezone;

document.querySelectorAll("[data-utc]").forEach((time) => {
  time.textContent = new Intl.DateTimeFormat("ru-RU", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(time.dataset.utc));
});

const calendarFilters = document.querySelectorAll("[data-calendar-filter]");
const calendarEvents = document.querySelectorAll("[data-calendar-type]");

calendarFilters.forEach((filter) => {
  filter.addEventListener("click", () => {
    const selectedType = filter.dataset.calendarFilter;

    calendarFilters.forEach((button) => {
      const isActive = button === filter;
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", String(isActive));
    });

    calendarEvents.forEach((eventCard) => {
      const types = eventCard.dataset.calendarType.split(" ");
      eventCard.hidden = selectedType !== "all" && !types.includes(selectedType);
    });
  });
});

const mediaFilters = document.querySelectorAll("[data-media-filter]");
const mediaItems = document.querySelectorAll("[data-media-item]");
const mediaSections = document.querySelectorAll("[data-media-section]");

mediaFilters.forEach((filter) => {
  filter.addEventListener("click", () => {
    const selectedType = filter.dataset.mediaFilter;

    mediaFilters.forEach((button) => {
      const isActive = button === filter;
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", String(isActive));
    });

    mediaItems.forEach((item) => {
      item.hidden = selectedType !== "all" && item.dataset.mediaType !== selectedType;
    });

    mediaSections.forEach((section) => {
      section.hidden = selectedType !== "all" && section.dataset.mediaSection !== selectedType;
    });
  });
});

const localTimeField = document.querySelector("[data-local-time]");
const mskTimeField = document.querySelector("[data-msk-time]");
const clockHourHand = document.querySelector("[data-clock-hour]");
const clockMinuteHand = document.querySelector("[data-clock-minute]");
const clockSecondHand = document.querySelector("[data-clock-second]");

function getTimeParts(date, timeZone) {
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).formatToParts(date);

  return Object.fromEntries(parts.map((part) => [part.type, part.value]));
}

function updateTimeWidget() {
  if (!localTimeField) return;

  const now = new Date();
  const localParts = getTimeParts(now, Intl.DateTimeFormat().resolvedOptions().timeZone);
  const mskParts = getTimeParts(now, "Europe/Moscow");
  localTimeField.textContent = `${localParts.hour}:${localParts.minute}`;
  if (mskTimeField) mskTimeField.textContent = mskParts.hour + ":" + mskParts.minute;

  const hours = Number(mskParts.hour) % 12;
  const minutes = Number(mskParts.minute);
  const seconds = Number(mskParts.second);
  const hourAngle = hours * 30 + minutes * 0.5 + seconds / 120;
  const minuteAngle = minutes * 6 + seconds * 0.1;
  const secondAngle = seconds * 6;

  if (clockHourHand) clockHourHand.style.transform = `rotate(${hourAngle}deg)`;
  if (clockMinuteHand) clockMinuteHand.style.transform = `rotate(${minuteAngle}deg)`;
  if (clockSecondHand) clockSecondHand.style.transform = `rotate(${secondAngle}deg)`;
}

updateTimeWidget();
setInterval(updateTimeWidget, 1000);

const languageButton = document.querySelector(".language-selector__button");
const languageMenu = document.querySelector(".language-selector__menu");
const languageLabel = document.querySelector("[data-language-label]");
const languageFlag = document.querySelector("[data-language-flag]");
const passwordToggle = document.querySelector(".auth-password-toggle");
const passwordInput = document.querySelector('input[name="password"]');

const authTranslations = {
  ru: {
    label: "Русский",
    back: "← Вернуться на сайт",
    welcome: "Добро пожаловать в личный кабинет",
    lead: "Войдите в аккаунт или зарегистрируйте новый.",
    note: "RaceManager ID — единая система аккаунтов для пилотов, команд и организаторов.",
    email: "Введите email",
    password: "Введите пароль",
    remember: "Запомнить устройство",
    login: "Войти",
    register: "Регистрация",
    forgot: "Забыли пароль?",
    restore: "Восстановить",
    loginSuccess: "Вы успешно вошли в аккаунт!",
    supportTitle: "Поддержка RaceManager",
    supportLead: "Опишите проблему или задайте вопрос. Мы сохраним обращение и свяжемся с вами по указанной почте.",
    supportNote: "Укажите как можно больше деталей: страницу, событие, аккаунт и что именно не работает.",
    supportName: "Ваше имя",
    supportSubject: "Тема обращения",
    supportCategory: "Выберите категорию",
    supportCategoryRegistration: "Регистрация на событие",
    supportCategoryAccount: "Личный кабинет",
    supportCategoryResults: "Результаты и таблицы",
    supportCategoryTech: "Техническая проблема",
    supportSubmit: "Отправить вопрос",
  },
  en: {
    label: "English",
    back: "← Back to website",
    welcome: "Welcome to your personal account",
    lead: "Sign in to your account or register a new one.",
    note: "RaceManager ID is a unified account system for drivers, teams and organizers.",
    email: "Enter email",
    password: "Enter password",
    remember: "Remember this device",
    login: "Sign in",
    register: "Register",
    forgot: "Forgot your password?",
    restore: "Restore",
    loginSuccess: "You have successfully signed in!",
    supportTitle: "RaceManager Support",
    supportLead: "Describe the issue or ask a question. We will save the request and contact you by email.",
    supportNote: "Add as many details as possible: page, event, account and what exactly does not work.",
    supportName: "Your name",
    supportSubject: "Request subject",
    supportCategory: "Select a category",
    supportCategoryRegistration: "Event registration",
    supportCategoryAccount: "Personal account",
    supportCategoryResults: "Results and tables",
    supportCategoryTech: "Technical issue",
    supportSubmit: "Send question",
  },
  de: {
    label: "Deutsch",
    back: "← Zurück zur Website",
    welcome: "Willkommen in Ihrem persönlichen Konto",
    lead: "Melden Sie sich an oder registrieren Sie ein neues Konto.",
    note: "RaceManager ID ist ein einheitliches Kontosystem für Fahrer, Teams und Veranstalter.",
    email: "E-Mail eingeben",
    password: "Passwort eingeben",
    remember: "Dieses Gerät merken",
    login: "Anmelden",
    register: "Registrieren",
    forgot: "Passwort vergessen?",
    restore: "Wiederherstellen",
    loginSuccess: "Sie haben sich erfolgreich angemeldet!",
    supportTitle: "RaceManager Support",
    supportLead: "Beschreiben Sie das Problem oder stellen Sie eine Frage. Wir speichern die Anfrage und kontaktieren Sie per E-Mail.",
    supportNote: "Geben Sie möglichst viele Details an: Seite, Event, Konto und was genau nicht funktioniert.",
    supportName: "Ihr Name",
    supportSubject: "Betreff der Anfrage",
    supportCategory: "Kategorie auswählen",
    supportCategoryRegistration: "Event-Registrierung",
    supportCategoryAccount: "Persönliches Konto",
    supportCategoryResults: "Ergebnisse und Tabellen",
    supportCategoryTech: "Technisches Problem",
    supportSubmit: "Frage senden",
  },
  pl: {
    label: "Polski",
    back: "← Wróć na stronę",
    welcome: "Witamy w koncie osobistym",
    lead: "Zaloguj się na konto lub zarejestruj nowe.",
    note: "RaceManager ID to wspólny system kont dla kierowców, zespołów i organizatorów.",
    email: "Wpisz email",
    password: "Wpisz hasło",
    remember: "Zapamiętaj urządzenie",
    login: "Zaloguj",
    register: "Rejestracja",
    forgot: "Nie pamiętasz hasła?",
    restore: "Odzyskaj",
    loginSuccess: "Zalogowano pomyślnie!",
    supportTitle: "Wsparcie RaceManager",
    supportLead: "Opisz problem lub zadaj pytanie. Zapiszemy zgłoszenie i skontaktujemy się mailowo.",
    supportNote: "Podaj jak najwięcej szczegółów: strona, wydarzenie, konto i co dokładnie nie działa.",
    supportName: "Twoje imię",
    supportSubject: "Temat zgłoszenia",
    supportCategory: "Wybierz kategorię",
    supportCategoryRegistration: "Rejestracja na wydarzenie",
    supportCategoryAccount: "Konto osobiste",
    supportCategoryResults: "Wyniki i tabele",
    supportCategoryTech: "Problem techniczny",
    supportSubmit: "Wyślij pytanie",
  },
};

function setAuthLanguage(language, flag) {
  const translations = authTranslations[language];
  if (!translations) return;

  document.documentElement.lang = language;
  if (languageLabel) languageLabel.textContent = translations.label;
  if (languageFlag) languageFlag.textContent = flag;
  document.querySelectorAll("[data-i18n]").forEach((element) => {
    const key = element.dataset.i18n;
    if (translations[key]) element.textContent = translations[key];
  });
  document.querySelectorAll("[data-i18n-placeholder]").forEach((element) => {
    const key = element.dataset.i18nPlaceholder;
    if (translations[key]) element.placeholder = translations[key];
  });
  document.querySelectorAll("[data-language]").forEach((option) => {
    option.classList.toggle("is-selected", option.dataset.language === language);
  });
  localStorage.setItem("authLanguage", language);
}

if (languageButton && languageMenu) {
  languageButton.addEventListener("click", () => {
    const isOpen = languageMenu.hidden;
    languageMenu.hidden = !isOpen;
    languageButton.setAttribute("aria-expanded", String(isOpen));
  });

  document.querySelectorAll("[data-language]").forEach((option) => {
    option.addEventListener("click", () => {
      setAuthLanguage(option.dataset.language, option.dataset.flag);
      languageMenu.hidden = true;
      languageButton.setAttribute("aria-expanded", "false");
    });
  });

  const savedLanguage = localStorage.getItem("authLanguage") || "ru";
  const savedOption = document.querySelector(`[data-language="${savedLanguage}"]`);
  setAuthLanguage(savedLanguage, savedOption?.dataset.flag || "🇷🇺");
}

if (passwordToggle && passwordInput) {
  passwordToggle.addEventListener("click", () => {
    passwordInput.type = passwordInput.type === "password" ? "text" : "password";
  });
}

const registerForm = document.querySelector(".register-form");
const registerSteps = document.querySelectorAll("[data-register-step]");
const registerProgress = document.querySelectorAll("[data-progress-step]");
const registerBackButton = document.querySelector("[data-register-back]");
const registerNextButton = document.querySelector("[data-register-next]");
const registerSubmitButton = document.querySelector("[data-register-submit]");
const registerError = document.querySelector("[data-register-error]");
let currentRegisterStep = 1;

const registerTranslations = {
  ru: {
    back: "← Вернуться ко входу", title: "Создание нового аккаунта", lead: "Заполните данные для регистрации в RaceManager.",
    step1: "Шаг 1. Данные аккаунта", step2: "Шаг 2. Безопасность", step3: "Шаг 3. Данные пилота",
    login: "Введите логин", email: "Введите email", password: "Введите пароль", confirmPassword: "Повторите пароль",
    lastName: "Фамилия", firstName: "Имя", middleName: "Отчество", phone: "Номер телефона", car: "Автомобиль (необязательно)",
    loginHint: "Минимум 4 символа в логине", passwordHint: "Пароль должен содержать минимум 6 символов", carHint: "Автомобиль можно добавить позже в личном кабинете.",
    previous: "Назад", next: "Продолжить", finish: "Зарегистрироваться", required: "Заполните обязательные поля.", loginTooShort: "Минимум 4 символа в логине.", emailInvalid: "Введите корректный email.", passwordTooShort: "Пароль должен содержать минимум 6 символов.", mismatch: "Пароли не совпадают.", success: "Вы успешно создали аккаунт!",
  },
  en: {
    back: "← Back to sign in", title: "Create a new account", lead: "Fill in the details to register with RaceManager.",
    step1: "Step 1. Account details", step2: "Step 2. Security", step3: "Step 3. Driver details",
    login: "Enter login", email: "Enter email", password: "Enter password", confirmPassword: "Repeat password",
    lastName: "Last name", firstName: "First name", middleName: "Middle name", phone: "Phone number", car: "Car (optional)",
    loginHint: "Login must contain at least 4 characters", passwordHint: "Password must contain at least 6 characters", carHint: "You can add a car later in your personal account.",
    previous: "Back", next: "Continue", finish: "Register", required: "Complete all required fields.", loginTooShort: "Login must contain at least 4 characters.", emailInvalid: "Enter a valid email.", passwordTooShort: "Password must contain at least 6 characters.", mismatch: "Passwords do not match.", success: "You have successfully created an account!",
  },
  de: {
    back: "← Zurück zur Anmeldung", title: "Neues Konto erstellen", lead: "Füllen Sie die Daten für die RaceManager-Registrierung aus.",
    step1: "Schritt 1. Kontodaten", step2: "Schritt 2. Sicherheit", step3: "Schritt 3. Fahrerdaten",
    login: "Login eingeben", email: "E-Mail eingeben", password: "Passwort eingeben", confirmPassword: "Passwort wiederholen",
    lastName: "Nachname", firstName: "Vorname", middleName: "Vatersname", phone: "Telefonnummer", car: "Fahrzeug (optional)",
    loginHint: "Der Login muss mindestens 4 Zeichen enthalten", passwordHint: "Das Passwort muss mindestens 6 Zeichen enthalten", carHint: "Sie können das Fahrzeug später im persönlichen Konto hinzufügen.",
    previous: "Zurück", next: "Weiter", finish: "Registrieren", required: "Füllen Sie alle Pflichtfelder aus.", loginTooShort: "Der Login muss mindestens 4 Zeichen enthalten.", emailInvalid: "Geben Sie eine gültige E-Mail-Adresse ein.", passwordTooShort: "Das Passwort muss mindestens 6 Zeichen enthalten.", mismatch: "Die Passwörter stimmen nicht überein.", success: "Sie haben erfolgreich ein Konto erstellt!",
  },
  pl: {
    back: "← Wróć do logowania", title: "Tworzenie nowego konta", lead: "Wypełnij dane do rejestracji w RaceManager.",
    step1: "Krok 1. Dane konta", step2: "Krok 2. Bezpieczeństwo", step3: "Krok 3. Dane kierowcy",
    login: "Wpisz login", email: "Wpisz email", password: "Wpisz hasło", confirmPassword: "Powtórz hasło",
    lastName: "Nazwisko", firstName: "Imię", middleName: "Drugie imię", phone: "Numer telefonu", car: "Samochód (opcjonalnie)",
    loginHint: "Login musi mieć co najmniej 4 znaki", passwordHint: "Hasło musi mieć co najmniej 6 znaków", carHint: "Samochód można dodać później w koncie osobistym.",
    previous: "Wstecz", next: "Dalej", finish: "Zarejestruj", required: "Wypełnij wymagane pola.", loginTooShort: "Login musi mieć co najmniej 4 znaki.", emailInvalid: "Wpisz poprawny email.", passwordTooShort: "Hasło musi mieć co najmniej 6 znaków.", mismatch: "Hasła nie są zgodne.", success: "Konto zostało utworzone!",
  },
};

function getRegisterLanguage() {
  return ["ru", "en", "de", "pl"].includes(document.documentElement.lang) ? document.documentElement.lang : "ru";
}

function applyRegisterTranslations(language) {
  const translations = registerTranslations[language];
  if (!translations) return;
  document.querySelectorAll("[data-register-i18n]").forEach((element) => {
    const key = element.dataset.registerI18n;
    if (translations[key]) element.textContent = translations[key];
  });
  document.querySelectorAll("[data-register-placeholder]").forEach((element) => {
    const key = element.dataset.registerPlaceholder;
    if (translations[key]) element.placeholder = translations[key];
  });
}

function showRegisterStep(step) {
  currentRegisterStep = step;
  registerSteps.forEach((element) => element.classList.toggle("is-active", Number(element.dataset.registerStep) === step));
  registerProgress.forEach((element) => {
    const progressStep = Number(element.dataset.progressStep);
    element.classList.toggle("is-active", progressStep === step);
    element.classList.toggle("is-complete", progressStep < step);
  });
  if (registerBackButton) registerBackButton.hidden = false;
  if (registerNextButton) registerNextButton.hidden = step === 3;
  if (registerSubmitButton) registerSubmitButton.hidden = step !== 3;
  if (registerError) registerError.hidden = true;
}

function showRegisterError(message) {
  if (!registerError) return;
  registerError.textContent = message;
  registerError.hidden = false;
}

function validateRegisterStep(step) {
  const translations = registerTranslations[getRegisterLanguage()];
  const stepElement = document.querySelector(`[data-register-step="${step}"]`);
  const requiredFields = stepElement?.querySelectorAll("[required]") || [];
  const fields = [...requiredFields];
  const emptyField = fields.find((field) => !String(field.value || "").trim());
  if (emptyField) {
    showRegisterError(translations.required);
    return false;
  }
  if (step === 1) {
    const login = registerForm?.elements.login;
    const email = registerForm?.elements.email;
    if (login && login.value.trim().length < Number(login.minLength || 4)) {
      showRegisterError(translations.loginTooShort);
      return false;
    }
    if (email && !email.checkValidity()) {
      showRegisterError(translations.emailInvalid);
      return false;
    }
  }
  if (step === 2) {
    const password = registerForm?.elements.registerPassword?.value;
    const confirmPassword = registerForm?.elements.confirmPassword?.value;
    if (password && password.length < 6) {
      showRegisterError(translations.passwordTooShort);
      return false;
    }
    if (password !== confirmPassword) {
      showRegisterError(translations.mismatch);
      return false;
    }
  }
  if (step === 3) {
    const phone = registerForm?.elements.phone;
    if (phone && !phone.checkValidity()) {
      showRegisterError("Введите телефон в формате " + supportedPhoneHint + ".");
      return false;
    }
  }
  return true;
}

if (registerForm) {
  showRegisterStep(1);
  applyRegisterTranslations(localStorage.getItem("authLanguage") || "ru");
  registerNextButton?.addEventListener("click", () => { if (validateRegisterStep(currentRegisterStep)) showRegisterStep(currentRegisterStep + 1); });
  registerBackButton?.addEventListener("click", () => {
    if (currentRegisterStep === 1) {
      window.location.href = "/pages/auth/login.html";
      return;
    }
    showRegisterStep(currentRegisterStep - 1);
  });
  registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!validateRegisterStep(3)) return;

    const translations = registerTranslations[getRegisterLanguage()];
    const payload = {
      login: registerForm.elements.login.value.trim(),
      email: registerForm.elements.email.value.trim().toLowerCase(),
      password: registerForm.elements.registerPassword.value,
      lastName: registerForm.elements.lastName.value.trim(),
      firstName: registerForm.elements.firstName.value.trim(),
      middleName: registerForm.elements.middleName.value.trim(),
      phone: registerForm.elements.phone.value.trim(),
      birthDate: registerForm.elements.birthDate.value,
      car: registerForm.elements.car.value.trim(),
    };

    try {
      const data = await postApiJson("/api/auth/register", payload);
      const user = data.user;
      localStorage.setItem(currentUserStorageKey, JSON.stringify(makeSessionUser(user)));
      if (registerError) registerError.hidden = true;
      showAuthToast(data.message || translations.success);
      registerForm.reset();
      setTimeout(() => { window.location.href = "/pages/account/account.html"; }, 3000);
      return;
    } catch (error) {
      if (registerError) {
        registerError.textContent = error.message || translations.required;
        registerError.hidden = false;
      }
    }
  });
  document.querySelectorAll("[data-language]").forEach((option) => option.addEventListener("click", () => applyRegisterTranslations(option.dataset.language)));
}


document.addEventListener("click", (event) => {
  if (!languageMenu || !languageButton) return;
  if (!event.target.closest(".language-selector")) {
    languageMenu.hidden = true;
    languageButton.setAttribute("aria-expanded", "false");
  }
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape" || !languageMenu || !languageButton) return;
  languageMenu.hidden = true;
  languageButton.setAttribute("aria-expanded", "false");
  languageButton.focus();
});

const authForm = document.querySelector(".auth-page:not(.support-page):not(.register-page) .auth-form");
const authMessage = document.querySelector("[data-auth-message]");
const authToast = document.querySelector("[data-auth-toast]");
let authToastTimer = null;
const currentUserStorageKey = "racemanagerCurrentUser";

function showAuthMessage(message, isSuccess = false) {
  if (!authMessage) return;
  authMessage.textContent = message;
  authMessage.classList.toggle("is-success", isSuccess);
  authMessage.hidden = false;
}

function showAuthToast(message) {
  if (!authToast) return;
  authToast.textContent = message;
  authToast.classList.add("is-visible");
  clearTimeout(authToastTimer);
  authToastTimer = setTimeout(() => authToast.classList.remove("is-visible"), 3000);
}

async function getSeedUsers() {
  try {
    const response = await fetch("/data/users.json");
    if (!response.ok) return [];
    const data = await response.json();
    return Array.isArray(data.users) ? data.users : [];
  } catch (error) {
    return [];
  }
}

async function postApiJson(path, payload) {
  const response = await fetch(path, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  let data = null;
  try {
    data = await response.json();
  } catch (error) {
    data = null;
  }
  if (!response.ok) {
    throw new Error(data?.message || "Не удалось выполнить запрос к серверу.");
  }
  return data;
}

function makeSessionUser(user) {
  return {
    id: user.id,
    login: user.login,
    email: user.email,
    role: user.role,
    profile: user.profile || {},
    statistics: user.statistics || { ranking: 1, points: 0, races: 0, wins: 0, podiums: 0, qualifications: 0, bestResult: "—", finishedEvents: 0 },
    championships: user.championships || [],
    team: user.team || null,
    vehicles: (user.vehicles || []).map((vehicle) => {
      const specs = vehicle.specs || {};
      return { ...vehicle, type: vehicle.type || specs["Тип"] || "", power: vehicle.power || specs["Мощность л.с."] || "", weight: vehicle.weight || specs["Вес, кг"] || "", powerToWeight: vehicle.powerToWeight || specs["Удельная мощность, л.с./т."] || "", drive: vehicle.drive || specs["Привод"] || "", engineType: vehicle.engineType || specs["Тип двигателя"] || "", engineModel: vehicle.engineModel || specs["Модель двигателя"] || "", engineVolume: vehicle.engineVolume || specs["Объем, см3"] || "", torque: vehicle.torque || specs["Крутящий момент, Нм"] || "" };
    }),
    applications: (user.applications || []).map((application) => ({
      ...application,
      name: application.name || application.eventName || "Событие RaceManager",
      type: application.type || application.discipline || "Событие"
    })),
    avatar: user.avatar || "",
  };
}

async function synchronizeServerSession() {
  try {
    const response = await fetch("/api/auth/session", { credentials: "include" });
    if (response.status === 401) {
      localStorage.removeItem(currentUserStorageKey);
      return;
    }
    if (!response.ok) return;

    const hadLocalSession = Boolean(getCurrentUser());
    const user = makeSessionUser(await response.json());
    localStorage.setItem(currentUserStorageKey, JSON.stringify(user));

    if (!hadLocalSession && !window.location.pathname.includes("/pages/auth/")) {
      window.location.reload();
    }
  } catch (error) {
    // Keep the last rendered state while the local API is restarting.
  }
}

const serverSessionPromise = synchronizeServerSession();

if (authForm) {
  authForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const email = authForm.elements.email.value.trim().toLowerCase();
    const password = authForm.elements.password.value;
    const translations = authTranslations[document.documentElement.lang] || authTranslations.ru;

    try {
      const data = await postApiJson("/api/auth/login", { email, password });
      localStorage.setItem(currentUserStorageKey, JSON.stringify(makeSessionUser(data.user)));
      if (authMessage) authMessage.hidden = true;
      showAuthToast(data.message || translations.loginSuccess);
      setTimeout(() => { window.location.href = "/pages/account/account.html"; }, 3000);
      return;
    } catch (error) {
      showAuthMessage(error.message || "Неверный email или пароль.");
    }
  });
}

const accountPage = document.querySelector(".account-page");
let accountUser = null;

function getCurrentUser() {
  try {
    return JSON.parse(localStorage.getItem(currentUserStorageKey) || "null");
  } catch (error) {
    return null;
  }
}

function saveCurrentUser(user) {
  localStorage.setItem(currentUserStorageKey, JSON.stringify(user));
}

async function ensureFreshAccountUser() {
  if (!accountUser?.id) {
    throw new Error("Сессия пользователя не найдена. Войдите в аккаунт заново.");
  }
  const response = await fetch("/api/auth/session", { credentials: "include", cache: "no-store" });
  if (response.status === 401) {
    localStorage.removeItem(currentUserStorageKey);
    throw new Error("Сессия истекла. Войдите в аккаунт заново.");
  }
  if (!response.ok) return accountUser;
  accountUser = makeSessionUser(await response.json());
  saveCurrentUser(accountUser);
  return accountUser;
}

function readImageFile(file, maxWidth = 1200) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.addEventListener("error", reject);
    reader.addEventListener("load", () => {
      const image = new Image();
      image.addEventListener("error", reject);
      image.addEventListener("load", () => {
        const scale = Math.min(1, maxWidth / image.width);
        const canvas = document.createElement("canvas");
        canvas.width = Math.round(image.width * scale);
        canvas.height = Math.round(image.height * scale);
        const context = canvas.getContext("2d");
        context.drawImage(image, 0, 0, canvas.width, canvas.height);
        resolve(canvas.toDataURL("image/jpeg", 0.82));
      });
      image.src = reader.result;
    });
    reader.readAsDataURL(file);
  });
}

function setAccountText(selector, value) {
  document.querySelectorAll(selector).forEach((element) => { element.textContent = value || "—"; });
}

function renderAccountStatistics(statistics = {}) {
  document.querySelectorAll("[data-account-stat]").forEach((element) => {
    const key = element.dataset.accountStat;
    element.textContent = statistics[key] ?? (key === "bestResult" ? "—" : "0");
  });
}

function renderAccountChampionships(championships = []) {
  const list = document.querySelector("[data-account-championships]");
  if (!list || !championships.length) return;
  list.replaceChildren();

  championships.forEach((championship) => {
    const card = document.createElement("article");
    card.className = "account-championship-card";
    card.style.setProperty("--championship-color", championship.color || "#e10600");
    card.innerHTML = `<div><small>${championship.discipline} · ${championship.season}</small><h4>${championship.name}</h4><span>${championship.status}</span></div><dl><div><dt>Позиция</dt><dd>${championship.position}</dd></div><div><dt>Очки</dt><dd>${championship.points}</dd></div><div><dt>Этапы</dt><dd>${championship.events}</dd></div></dl>`;
    list.append(card);
  });
}

function renderAccountTeam(team, user) {
  const container = document.querySelector("[data-account-team]");
  if (!container) return;
  const profile = user?.profile || {};
  const displayedTeams = [];
  if (user?.role === "Организатор" && profile.organizationName) displayedTeams.push({ name: profile.organizationName, role: "Команда организаторов", color: profile.organizationColor, logo: profile.organizationLogo, banner: profile.organizationBanner, membersCount: (profile.organizationMembers || []).length });
  if (profile.racingTeamName) displayedTeams.push({ name: profile.racingTeamName, role: "Гоночная команда", color: profile.racingTeamColor, logo: profile.racingTeamLogo, banner: profile.racingTeamBanner, membersCount: (profile.racingTeamMembers || []).length });
  (profile.teamMemberships || []).forEach((membership) => displayedTeams.push({
    name: membership.teamName,
    role: membership.teamType === "Organizer" ? "Команда организаторов" : "Гоночная команда",
    color: membership.teamColor,
    logo: membership.teamLogo,
    banner: membership.teamBanner,
    members: "Вы состоите в этой команде."
  }));
  if (!displayedTeams.length && team) displayedTeams.push(team);
  if (!displayedTeams.length) return;
  container.className = "account-team-list";
  container.innerHTML = displayedTeams.map((displayedTeam) => {
    const logo = displayedTeam.logo ? '<img src="' + displayedTeam.logo + '" alt="Логотип команды">' : "<span>RM</span>";
    const memberText = displayedTeam.membersCount !== undefined ? "Участников: " + displayedTeam.membersCount : (displayedTeam.members || "Команда участвует в командном зачёте текущего сезона.");
    const stats = displayedTeam.membersCount !== undefined ? '<dl><div><dt>Состав</dt><dd>' + displayedTeam.membersCount + '</dd></div></dl>' : '<dl><div><dt>Позиция</dt><dd>' + (displayedTeam.position || "—") + '</dd></div><div><dt>Очки</dt><dd>' + (displayedTeam.points || 0) + '</dd></div></dl>';
    const link = displayedTeam.page ? '<a class="button button--dark button--small" href="' + displayedTeam.page + '">Подробнее</a>' : "";
    const background = displayedTeam.banner ? 'linear-gradient(90deg, rgba(255,255,255,.96), rgba(255,255,255,.82)), url(&quot;' + displayedTeam.banner + '&quot;)' : "none";
    return '<article class="account-team-card" style="--team-color:' + (displayedTeam.color || "#e10600") + ';background-image:' + background + '"><div class="account-team-card__logo">' + logo + '</div><div class="account-team-card__copy"><small>' + displayedTeam.role + '</small><h4>' + escapeHtml(displayedTeam.name) + '</h4><p>' + memberText + '</p></div>' + stats + link + '</article>';
  }).join("");
}

function teamMembersSignature(user) {
  const profile = user?.profile || {};
  return JSON.stringify([profile.organizationMembers || [], profile.racingTeamMembers || []]);
}

function refreshTeamEditorsIfChanged(previousUser, freshUser) {
  if (!accountPage || teamMembersSignature(previousUser) === teamMembersSignature(freshUser)) return;
  renderOrganizationTeam(freshUser);
  renderRacingTeam(freshUser);
}

function pendingTeamInvitationIds(user) {
  return new Set((user?.profile?.teamInvitations || []).filter((item) => item.status === "Pending").map((item) => item.id));
}

async function refreshAccountTeamState(announce = true) {
  if (!accountPage || !accountUser?.id) return;
  const previousIds = pendingTeamInvitationIds(accountUser);
  try {
    const response = await fetch("/api/users/" + encodeURIComponent(accountUser.id), { credentials: "include", cache: "no-store" });
    if (!response.ok) return;
    const freshUser = makeSessionUser(await response.json());
    const newInvitations = (freshUser.profile?.teamInvitations || []).filter((item) => item.status === "Pending" && !previousIds.has(item.id));
    const previousUser = accountUser;
    accountUser = freshUser;
    saveCurrentUser(accountUser);
    refreshTeamEditorsIfChanged(previousUser, accountUser);
    renderTeamInvitationCenter(accountUser);
    renderAccountTeam(accountUser.team, accountUser);
    if (announce && newInvitations.length) {
      const invitation = newInvitations[0];
      showSiteToast(invitation.teamType === "Judge"
        ? "Новое приглашение судить событие «" + invitation.teamName + "»"
        : "Новое приглашение в команду «" + invitation.teamName + "»");
    }
  } catch (error) {
    // Фоновое обновление не должно мешать работе личного кабинета.
  }
}

if (accountPage) {
  const user = getCurrentUser();
  accountUser = user;
  if (!user) {
    window.location.href = "/pages/auth/login.html";
  } else {
    const profile = user.profile || {};
    const fullName = [profile.lastName, profile.firstName, profile.middleName].filter(Boolean).join(" ") || user.login;
    const initials = [profile.firstName, profile.lastName].filter(Boolean).map((part) => part[0]).join("").toUpperCase() || "RM";
    const greeting = profile.firstName || user.login || "Пилот";

    setAccountText("[data-account-initials]", initials);
    setAccountText("[data-account-login]", user.login);
    setAccountText("[data-account-email]", user.email);
    setAccountText("[data-account-full-name]", fullName);
    setAccountText("[data-account-greeting]", greeting);
    setAccountText("[data-account-role]", user.role);
    setAccountText("[data-account-last-name]", profile.lastName);
    setAccountText("[data-account-first-name]", profile.firstName);
    setAccountText("[data-account-middle-name]", profile.middleName);
    setAccountText("[data-account-phone]", profile.phone);
    setAccountText("[data-account-driver-number]", profile.driverNumber || "—");
    setAccountText("[data-account-birth-date]", profile.birthDate);
    const favoriteVehicle = (user.vehicles || []).find((vehicle) => vehicle.id === user.favoriteVehicleId) || (user.vehicles || [])[0];
    if (favoriteVehicle && !user.favoriteVehicleId) user.favoriteVehicleId = favoriteVehicle.id;
    setAccountText("[data-account-car]", favoriteVehicle?.name || profile.car || "Автомобиль не указан");
    setAccountText("[data-account-team-name]", user.team?.name || "Не состоит в команде");
    renderAccountStatistics(user.statistics);
    renderAccountChampionships(user.championships);
    renderAccountTeam(user.team, user);
    renderAccountVehicles(user.vehicles || []);
    renderAccountApplications(user.applications || []);
    notifyRejectedApplications(user.applications || []);
    renderTeamInvitationCenter(user);
    refreshAccountTeamState();
    populateAccountSettings(user);
    renderOrganizationTeam(user);
    renderRacingTeam(user);
    if (user.role !== "Организатор") {
      selectTeamKind("racing");
      const title = document.querySelector("[data-team-view-title]");
      const copy = document.querySelector("[data-team-view-copy]");
      if (title) title.textContent = "Создание гоночной команды";
      if (copy) copy.textContent = "Настройте гоночную команду, приглашайте пилотов и добавляйте командные автомобили.";
    }
    renderAccountAvatar(user.avatar);
  }
}

function renderAccountAvatar(avatar) {
  if (!avatar) return;
  document.querySelectorAll("[data-account-initials]").forEach((element) => {
    element.textContent = "";
    element.style.backgroundImage = `url("${avatar}")`;
    element.style.backgroundSize = "cover";
    element.style.backgroundPosition = "center";
  });
  const preview = document.querySelector("[data-account-avatar-preview]");
  if (preview) preview.style.backgroundImage = `url("${avatar}")`;
}

function vehicleValue(value, unit = "") {
  return value ? `${value}${unit ? " " + unit : ""}` : "Не указано";
}

function renderAccountVehicles(vehicles = []) {
  const grid = document.querySelector("[data-account-vehicles]");
  if (!grid) return;
  grid.replaceChildren();

  if (!vehicles.length) {
    grid.innerHTML = '<div class="account-empty-state account-empty-state--large account-vehicle-empty"><strong>Автомобили не добавлены.</strong><p>Добавьте автомобиль, чтобы он появился в профиле и был доступен при регистрации.</p></div>';
    return;
  }

  vehicles.forEach((vehicle) => {
    const card = document.createElement("article");
    const isFavorite = accountUser?.favoriteVehicleId === vehicle.id;
    card.className = "account-vehicle-card" + (isFavorite ? " is-favorite" : "");
    const teamBadge = vehicle.isTeamVehicle ? `<span class="account-vehicle-card__team">${vehicle.teamLogo ? `<img src="${vehicle.teamLogo}" alt="">` : ""}<b>${escapeHtml(vehicle.teamName || "Команда")}</b></span>` : "";
    const imageMarkup = vehicle.image ? `<img src="${vehicle.image}" alt="${vehicle.name}">` : `<span>RM</span>`;
    card.innerHTML = `<button class="account-vehicle-card__preview" type="button" data-vehicle-details-open>${imageMarkup}<div><small>${vehicleValue(vehicle.type)}</small><h3>${vehicle.name}</h3>${teamBadge}<p>${vehicleValue(vehicle.power, "л.с.")} · ${vehicle.drive ? vehicle.drive + " привод" : "Привод не указан"}</p></div></button><div class="account-vehicle-card__actions"><button class="account-vehicle-card__favorite" type="button" data-set-favorite-vehicle="${vehicle.id}" title="${isFavorite ? "Автомобиль отображается в профиле" : "Сделать избранным"}" aria-label="${isFavorite ? "Автомобиль отображается в профиле" : "Сделать избранным"}">${isFavorite ? "★" : "☆"}</button><button class="account-vehicle-card__delete" type="button" data-delete-vehicle="${vehicle.id}" title="Удалить автомобиль" aria-label="Удалить автомобиль">×</button></div>`;
    card.querySelector("[data-vehicle-details-open]").addEventListener("click", () => openVehicleDetails(vehicle));
    card.querySelector("[data-set-favorite-vehicle]").addEventListener("click", () => {
      if (!accountUser) return;
      const shouldUnsetFavorite = accountUser.favoriteVehicleId === vehicle.id;
      accountUser.favoriteVehicleId = shouldUnsetFavorite ? "" : vehicle.id;
      accountUser.profile = { ...(accountUser.profile || {}), car: shouldUnsetFavorite ? "" : vehicle.name };
      saveCurrentUser(accountUser);
      setAccountText("[data-account-car]", shouldUnsetFavorite ? "Автомобиль не указан" : vehicle.name);
      renderAccountVehicles(accountUser.vehicles || []);
      showSiteToast(shouldUnsetFavorite ? "Автомобиль убран из избранного" : "Избранный автомобиль обновлён");
    });
    card.querySelector("[data-delete-vehicle]").addEventListener("click", async () => {
      if (!accountUser) return;
      const confirmed = window.confirm(`Удалить автомобиль «${vehicle.name}»?`);
      if (!confirmed) return;
      try {
        const response = await fetch("/api/account/" + encodeURIComponent(accountUser.id) + "/vehicles/" + encodeURIComponent(vehicle.id), { method: "DELETE", credentials: "include" });
        const data = await response.json().catch(() => null);
        if (!response.ok) throw new Error(data?.message || "Не удалось удалить автомобиль из базы данных.");
        accountUser = makeSessionUser(data.user);
        saveCurrentUser(accountUser);
        const currentFavorite = (accountUser.vehicles || []).find((item) => item.id === accountUser.favoriteVehicleId);
        setAccountText("[data-account-car]", currentFavorite?.name || accountUser.profile?.car || "Автомобиль не указан");
        renderAccountVehicles(accountUser.vehicles || []);
        showSiteToast("Автомобиль удалён");
      } catch (error) {
        showSiteToast(error.message || "Не удалось удалить автомобиль", "error");
      }
    });
    grid.append(card);
  });
}

function renderAccountApplications(applications = []) {
  const container = document.querySelector("[data-account-applications]");
  if (!container || !applications.length) return;
  container.replaceChildren();
  applications.forEach((application) => {
    const card = document.createElement("article");
    card.className = "account-application-card";
    card.innerHTML = `<div class="account-application-card__date">${application.date}</div><div class="account-application-card__name"><strong>${application.name}</strong><span>${application.location}</span></div><div class="account-application-card__type">${application.type}<small>${application.status || "Заявка подана"}</small></div><a href="${application.href || "/calendar.html"}">Подробнее <span>›</span></a>`;
    container.append(card);
  });
}


function notifyRejectedApplications(applications = []) {
  const rejected = applications.find((application) =>
    String(application.status || "").toLowerCase().includes("отклон"));
  if (!rejected) return;
  const notificationKey = `racemanager.rejection-notified.${rejected.id || rejected.eventKey || rejected.name}`;
  if (localStorage.getItem(notificationKey)) return;
  localStorage.setItem(notificationKey, "true");
  showSiteToast(`Ваша заявка на событие «${rejected.name || rejected.eventName || "RaceManager"}» не одобрена организатором`, "error");
}

function populateAccountSettings(user) {
  const form = document.querySelector("[data-account-settings-form]");
  if (!form) return;
  const profile = user.profile || {};
  form.elements.lastName.value = profile.lastName || "";
  form.elements.firstName.value = profile.firstName || "";
  form.elements.middleName.value = profile.middleName || "";
  form.elements.email.value = user.email || "";
  form.elements.phone.value = profile.phone || "";
  if (form.elements.driverNumber) form.elements.driverNumber.value = profile.driverNumber || "";
}


function teamMemberFieldNames(teamType) {
  return teamType === "Organizer"
    ? { name: "teamMemberName", phone: "teamMemberPhone", email: "teamMemberEmail" }
    : { name: "racingTeamMemberName", phone: "racingTeamMemberPhone", email: "racingTeamMemberEmail" };
}

function createTeamMemberRow(member = {}, teamType = "Racing") {
  const fields = teamMemberFieldNames(teamType);
  const stored = Boolean(member.userId || member.status);
  const row = document.createElement("div");
  row.className = "organization-member-row";
  row.dataset.memberStored = stored ? "true" : "false";
  row.dataset.memberUserId = member.userId || "";
  row.dataset.memberStatus = member.status || "";
  const status = member.status === "Owner" ? "Владелец" : (member.status === "Accepted" ? "Одобрено" : (member.status === "Declined" ? "Отклонено" : (member.status === "Pending" ? "Ожидает" : "Новый")));
  row.innerHTML = '<input type="text" name="' + fields.name + '" placeholder="Фамилия Имя" aria-label="Фамилия и имя участника">' +
    '<input type="tel" name="' + fields.phone + '" placeholder="+375 29 123 45 67" aria-label="Телефон участника">' +
    '<input type="email" name="' + fields.email + '" placeholder="name@example.com" aria-label="Почта участника">' +
    '<span class="organization-member-status organization-member-status--' + (member.status || "new").toLowerCase() + '">' + status + '</span>' +
    '<button type="button" data-remove-team-member-row aria-label="Удалить строку">×</button>';
  row.querySelector('[name="' + fields.name + '"]').value = member.fullName || "";
  row.querySelector('[name="' + fields.phone + '"]').value = member.phone || "";
  row.querySelector('[name="' + fields.email + '"]').value = member.email || "";
  if (member.status === "Owner") {
    row.querySelectorAll("input").forEach((input) => { input.readOnly = true; });
    row.querySelector("[data-remove-team-member-row]").hidden = true;
  }
  row.querySelector("[data-remove-team-member-row]").addEventListener("click", () => {
    if (row.dataset.memberStored === "true") {
      showSiteToast("Сохраните изменения, чтобы удалить участника из таблицы");
    }
    row.remove();
  });
  return row;
}

function teamMemberRowValue(row, teamType) {
  const fields = teamMemberFieldNames(teamType);
  return {
    userId: row.dataset.memberUserId || "",
    status: row.dataset.memberStatus || "",
    fullName: row.querySelector('[name="' + fields.name + '"]').value.trim(),
    phone: row.querySelector('[name="' + fields.phone + '"]').value.trim(),
    email: row.querySelector('[name="' + fields.email + '"]').value.trim().toLowerCase()
  };
}

function applyTeamPreview(profile = {}) {
  const name = document.querySelector("[data-team-preview-name]");
  const banner = document.querySelector("[data-team-preview-banner]");
  const logo = document.querySelector("[data-team-preview-logo]");
  if (name) name.textContent = profile.organizationName || "Команда не настроена";
  if (banner) {
    banner.style.setProperty("--organization-color", profile.organizationColor || "#e10600");
    banner.style.backgroundImage = profile.organizationBanner ? 'linear-gradient(90deg, rgba(7,8,12,.92), rgba(7,8,12,.32)), url("' + profile.organizationBanner + '")' : "";
  }
  if (logo) logo.innerHTML = profile.organizationLogo ? '<img src="' + profile.organizationLogo + '" alt="Логотип команды">' : "<span>RM</span>";
}

function applyRacingTeamPreview(profile = {}) {
  const name = document.querySelector("[data-racing-team-preview-name]");
  const banner = document.querySelector("[data-racing-team-preview-banner]");
  const logo = document.querySelector("[data-racing-team-preview-logo]");
  if (name) name.textContent = profile.racingTeamName || "Команда не настроена";
  if (banner) {
    banner.style.setProperty("--organization-color", profile.racingTeamColor || "#e10600");
    banner.style.backgroundImage = profile.racingTeamBanner ? 'linear-gradient(90deg, rgba(7,8,12,.92), rgba(7,8,12,.32)), url("' + profile.racingTeamBanner + '")' : "";
  }
  if (logo) logo.innerHTML = profile.racingTeamLogo ? '<img src="' + profile.racingTeamLogo + '" alt="Логотип гоночной команды">' : "<span>RM</span>";
}

function renderTeamEditor(user, teamType) {
  const isOrganizerTeam = teamType === "Organizer";
  const form = document.querySelector(isOrganizerTeam ? "[data-organization-team-form]" : "[data-racing-team-form]");
  if (!form) return;
  const profile = user?.profile || {};
  const list = form.querySelector(isOrganizerTeam ? "[data-team-member-list]" : "[data-racing-team-member-list]");
  const members = isOrganizerTeam ? (profile.organizationMembers || []) : (profile.racingTeamMembers || []);
  const nameField = isOrganizerTeam ? "organizationName" : "racingTeamName";
  const colorField = isOrganizerTeam ? "organizationColor" : "racingTeamColor";
  form.elements[nameField].value = profile[nameField] || "";
  form.elements[colorField].value = profile[colorField] || "#e10600";
  list.replaceChildren();
  members.forEach((member) => list.append(createTeamMemberRow(member, teamType)));
  list.append(createTeamMemberRow({}, teamType));
  if (isOrganizerTeam) applyTeamPreview(profile);
  else applyRacingTeamPreview(profile);
}

function renderOrganizationTeam(user) {
  renderTeamEditor(user, "Organizer");
}

function renderRacingTeam(user) {
  renderTeamEditor(user, "Racing");
}

function selectTeamKind(kind) {
  document.querySelectorAll("[data-team-kind-tab]").forEach((button) => button.classList.toggle("is-active", button.dataset.teamKindTab === kind));
  document.querySelectorAll("[data-team-kind-panel]").forEach((panel) => { panel.hidden = panel.dataset.teamKindPanel !== kind; });
}

document.querySelectorAll("[data-team-kind-tab]").forEach((button) => button.addEventListener("click", () => selectTeamKind(button.dataset.teamKindTab)));

document.querySelector("[data-organization-team-form]")?.addEventListener("input", (event) => {
  if (!accountUser || !["organizationName", "organizationColor"].includes(event.target.name)) return;
  applyTeamPreview({ ...accountUser.profile, organizationName: event.currentTarget.elements.organizationName.value.trim(), organizationColor: event.currentTarget.elements.organizationColor.value });
});

document.querySelector("[data-racing-team-form]")?.addEventListener("input", (event) => {
  if (!accountUser || !["racingTeamName", "racingTeamColor"].includes(event.target.name)) return;
  applyRacingTeamPreview({ ...accountUser.profile, racingTeamName: event.currentTarget.elements.racingTeamName.value.trim(), racingTeamColor: event.currentTarget.elements.racingTeamColor.value });
});

async function saveTeamEditor(form, teamType, inviteRequested) {
  const isOrganizerTeam = teamType === "Organizer";
  const profile = accountUser.profile || {};
  const logoField = isOrganizerTeam ? "organizationLogo" : "racingTeamLogo";
  const bannerField = isOrganizerTeam ? "organizationBanner" : "racingTeamBanner";
  const nameField = isOrganizerTeam ? "organizationName" : "racingTeamName";
  const colorField = isOrganizerTeam ? "organizationColor" : "racingTeamColor";
  const memberListSelector = isOrganizerTeam ? "[data-team-member-list]" : "[data-racing-team-member-list]";
  const existingLogo = isOrganizerTeam ? profile.organizationLogo : profile.racingTeamLogo;
  const existingBanner = isOrganizerTeam ? profile.organizationBanner : profile.racingTeamBanner;
  const logoFile = form.elements[logoField].files[0];
  const bannerFile = form.elements[bannerField].files[0];
  const logo = logoFile ? await readImageFile(logoFile, 900) : existingLogo || "";
  const banner = bannerFile ? await readImageFile(bannerFile, 1600) : existingBanner || "";
  const rows = [...form.querySelectorAll(memberListSelector + " .organization-member-row")];
  const storedMembers = rows.filter((row) => row.dataset.memberStored === "true").map((row) => teamMemberRowValue(row, teamType));
  const invitees = inviteRequested
    ? rows.filter((row) => row.dataset.memberStored !== "true").map((row) => teamMemberRowValue(row, teamType)).filter((member) => member.fullName || member.phone || member.email)
    : [];
  if (inviteRequested && !invitees.length) throw new Error("Введите фамилию и имя, телефон или почту пользователя.");

  const payload = {
    lastName: profile.lastName || "",
    firstName: profile.firstName || "",
    middleName: profile.middleName || "",
    email: accountUser.email,
    phone: profile.phone || "",
    avatar: accountUser.avatar || "",
    driverNumber: profile.driverNumber || "",
    organizationName: isOrganizerTeam ? form.elements[nameField].value.trim() : profile.organizationName || "",
    organizationColor: isOrganizerTeam ? form.elements[colorField].value || "#e10600" : profile.organizationColor || "#e10600",
    organizationLogo: isOrganizerTeam ? logo : profile.organizationLogo || "",
    organizationBanner: isOrganizerTeam ? banner : profile.organizationBanner || "",
    organizationMembers: isOrganizerTeam ? storedMembers : profile.organizationMembers || [],
    racingTeamName: isOrganizerTeam ? profile.racingTeamName || "" : form.elements[nameField].value.trim(),
    racingTeamColor: isOrganizerTeam ? profile.racingTeamColor || "#e10600" : form.elements[colorField].value || "#e10600",
    racingTeamLogo: isOrganizerTeam ? profile.racingTeamLogo || "" : logo,
    racingTeamBanner: isOrganizerTeam ? profile.racingTeamBanner || "" : banner,
    racingTeamMembers: isOrganizerTeam ? profile.racingTeamMembers || [] : storedMembers
  };

  const response = await fetch("/api/account/" + encodeURIComponent(accountUser.id) + "/profile", {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось сохранить изменения.");
  accountUser = makeSessionUser(data.user);
  saveCurrentUser(accountUser);

  let missingUsers = [];
  if (invitees.length) {
    const invitationData = await sendTeamInvitations(teamType, invitees);
    missingUsers = invitationData.missingUsers || [];
    accountUser = makeSessionUser(invitationData.user);
    saveCurrentUser(accountUser);
  }

  if (isOrganizerTeam) renderOrganizationTeam(accountUser);
  else renderRacingTeam(accountUser);
  renderAccountTeam(accountUser.team, accountUser);
  const message = document.querySelector(isOrganizerTeam ? "[data-team-settings-message]" : "[data-racing-team-settings-message]");
  if (message) {
    message.textContent = inviteRequested ? "Приглашение обработано" : "Изменения сохранены";
    message.hidden = false;
    setTimeout(() => { message.hidden = true; }, 3000);
  }
  if (missingUsers.length) {
    showSiteToast("Пользователь не найден: " + missingUsers.join(", "), "error");
  } else {
    showSiteToast(invitees.length ? "Пользователь найден, приглашение отправлено" : "Изменения команды сохранены");
  }
}

document.querySelector("[data-organization-team-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!accountUser) return;
  const inviteRequested = event.currentTarget.dataset.inviteRequested === "true";
  delete event.currentTarget.dataset.inviteRequested;
  try { await saveTeamEditor(event.currentTarget, "Organizer", inviteRequested); }
  catch (error) { showSiteToast(error.message || "Не удалось сохранить изменения", "error"); }
});

document.querySelector("[data-racing-team-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!accountUser) return;
  const inviteRequested = event.currentTarget.dataset.inviteRequested === "true";
  delete event.currentTarget.dataset.inviteRequested;
  try { await saveTeamEditor(event.currentTarget, "Racing", inviteRequested); }
  catch (error) { showSiteToast(error.message || "Не удалось сохранить изменения", "error"); }
});

async function inviteFromTeamEditor(teamType) {
  if (!accountUser) return;
  const form = document.querySelector(teamType === "Organizer" ? "[data-organization-team-form]" : "[data-racing-team-form]");
  if (!form) return;
  if (!form.reportValidity()) {
    showSiteToast("Проверьте название команды и формат заполненных полей", "error");
    return;
  }
  const button = form.querySelector(teamType === "Organizer" ? "[data-invite-team-member]" : "[data-invite-racing-team-member]");
  if (button) button.disabled = true;
  try {
    await saveTeamEditor(form, teamType, true);
  } catch (error) {
    showSiteToast(error.message || "Не удалось отправить приглашение", "error");
  } finally {
    if (button) button.disabled = false;
  }
}

document.querySelector("[data-invite-team-member]")?.addEventListener("click", () => inviteFromTeamEditor("Organizer"));
document.querySelector("[data-invite-racing-team-member]")?.addEventListener("click", () => inviteFromTeamEditor("Racing"));

async function sendTeamInvitations(teamType, invitees) {
  if (!invitees.length || !accountUser) return { missingUsers: [], user: accountUser };
  const response = await fetch("/api/account/" + encodeURIComponent(accountUser.id) + "/team-invitations", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ teamType, invitees })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось отправить приглашение.");
  return data;
}

function invitationTypeLabel(item) {
  if (item.teamType === "Judge") return "Приглашение судьи";
  return item.teamType === "Organizer" ? "Команда организаторов" : "Гоночная команда";
}

function invitationMessage(item) {
  const owner = item.ownerName || (item.teamType === "Judge" ? "Организатор" : "Лидер команды");
  return item.teamType === "Judge"
    ? `${owner} приглашает вас судить событие.`
    : `${owner} приглашает вас присоединиться.`;
}

function renderTeamInvitationCenter(user) {
  const center = document.querySelector("[data-team-invitation-center]");
  const invitationList = document.querySelector("[data-team-invitation-list]");
  const notificationList = document.querySelector("[data-team-notification-list]");
  if (!center || !invitationList || !notificationList) return;
  const profile = user?.profile || {};
  const invitations = (profile.teamInvitations || []).filter((item) => item.status === "Pending");
  const notifications = profile.teamNotifications || [];
  center.hidden = invitations.length === 0 && notifications.length === 0;
  invitationList.innerHTML = invitations.map((item) =>
    '<article class="team-invitation-card"><div class="team-invitation-card__logo">' +
    (item.teamLogo ? '<img src="' + item.teamLogo + '" alt="">' : "<span>RM</span>") +
    '</div><div><small>' + escapeHtml(invitationTypeLabel(item)) +
    '</small><strong>' + escapeHtml(item.teamName) + '</strong><p>' + escapeHtml(invitationMessage(item)) +
    '</p></div><div class="team-invitation-card__actions"><button class="team-invite-accept" type="button" data-team-invite-response="' +
    escapeHtml(item.id) + '" data-accept="true" aria-label="Принять приглашение">✓</button><button class="team-invite-decline" type="button" data-team-invite-response="' +
    escapeHtml(item.id) + '" data-accept="false" aria-label="Отклонить приглашение">×</button></div></article>'
  ).join("");
  notificationList.innerHTML = notifications.map((item) => '<p><span>i</span>' + escapeHtml(item.message) + '</p>').join("");
  invitationList.querySelectorAll("[data-team-invite-response]").forEach((button) => {
    button.addEventListener("click", () => respondTeamInvitation(button.dataset.teamInviteResponse, button.dataset.accept === "true"));
  });
}

async function respondTeamInvitation(invitationId, accept) {
  if (!accountUser) return;
  try {
    const response = await fetch("/api/account/" + encodeURIComponent(accountUser.id) + "/team-invitations/" + encodeURIComponent(invitationId) + "/respond", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accept })
    });
    const data = await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || "Не удалось обработать приглашение.");
    accountUser = makeSessionUser(data.user);
    saveCurrentUser(accountUser);
    renderTeamInvitationCenter(accountUser);
    renderAccountTeam(accountUser.team, accountUser);
    showSiteToast(data.message);
  } catch (error) {
    showSiteToast(error.message || "Не удалось обработать приглашение", "error");
  }
}

function openAccountModal(modal) {
  if (!modal) return;
  modal.classList.add("is-open");
  modal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
  const dialog = modal.querySelector(".account-modal__dialog");
  if (dialog) dialog.scrollTop = 0;
  requestAnimationFrame(() => centerVisibleModal(modal));
}

function closeAccountModal(modal) {
  if (!modal) return;
  modal.classList.remove("is-open");
  modal.setAttribute("aria-hidden", "true");
  document.body.classList.remove("modal-open");
}

function openVehicleDetails(vehicle) {
  const modal = document.querySelector("[data-vehicle-details-modal]");
  const content = document.querySelector("[data-vehicle-details]");
  if (!modal || !content) return;
  const imageMarkup = vehicle.image
    ? `<img class="vehicle-details__image" src="${vehicle.image}" alt="${escapeHtml(vehicle.name)}">`
    : `<div class="vehicle-details__image vehicle-details__image--empty">RM</div>`;
  content.innerHTML = `${imageMarkup}<div class="account-modal__head"><span class="eyebrow">${vehicleValue(vehicle.type)}</span><h2>${vehicle.name}</h2></div><dl class="vehicle-details__grid"><div><dt>Мощность</dt><dd>${vehicleValue(vehicle.power, "л.с.")}</dd></div><div><dt>Вес</dt><dd>${vehicleValue(vehicle.weight, "кг")}</dd></div><div><dt>Удельная мощность</dt><dd>${vehicleValue(vehicle.powerToWeight, "л.с./т.")}</dd></div><div><dt>Привод</dt><dd>${vehicleValue(vehicle.drive)}</dd></div><div><dt>Тип двигателя</dt><dd>${vehicleValue(vehicle.engineType)}</dd></div><div><dt>Модель двигателя</dt><dd>${vehicleValue(vehicle.engineModel)}</dd></div><div><dt>Объем</dt><dd>${vehicleValue(vehicle.engineVolume, "см³")}</dd></div><div><dt>Крутящий момент</dt><dd>${vehicleValue(vehicle.torque, "Нм")}</dd></div></dl>`;
  openAccountModal(modal);
}

function switchAccountTab(selected) {
  document.querySelectorAll("[data-account-tab]").forEach((button) => button.classList.toggle("is-active", button.dataset.accountTab === selected));
  document.querySelectorAll("[data-account-view]").forEach((view) => { view.hidden = view.dataset.accountView !== selected; });
}

document.querySelectorAll("[data-account-tab]").forEach((tab) => {
  tab.addEventListener("click", () => switchAccountTab(tab.dataset.accountTab));
});

document.querySelector("[data-account-edit-profile]")?.addEventListener("click", () => {
  switchAccountTab("settings");
  document.querySelector("[data-account-settings-form]")?.scrollIntoView({ behavior: "smooth", block: "start" });
});

if (accountPage && window.location.hash === "#settings") {
  switchAccountTab("settings");
}

const vehicleModal = document.querySelector("[data-vehicle-modal]");
document.querySelectorAll("[data-open-vehicle-modal]").forEach((button) => button.addEventListener("click", () => openAccountModal(vehicleModal)));
document.querySelectorAll("[data-close-vehicle-modal]").forEach((button) => button.addEventListener("click", () => closeAccountModal(vehicleModal)));
document.querySelectorAll("[data-close-vehicle-details]").forEach((button) => button.addEventListener("click", () => closeAccountModal(document.querySelector("[data-vehicle-details-modal]"))));

document.querySelector("[data-vehicle-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const submitButton = form.querySelector("button[type=\"submit\"]");
  try {
    await ensureFreshAccountUser();
    if (submitButton) {
      submitButton.disabled = true;
      submitButton.textContent = "Сохраняем...";
    }
    const imageFile = form.elements.image.files[0];
    const image = imageFile ? await readImageFile(imageFile) : "";
    const isTeamVehicle = Boolean(form.elements.isTeamVehicle?.checked);
    const teamType = form.elements.teamType?.value || "Racing";
    const specs = {
      "Тип": form.elements.type.value.trim(),
      "Мощность л.с.": form.elements.power.value.trim(),
      "Вес, кг": form.elements.weight.value.trim(),
      "Удельная мощность, л.с./т.": form.elements.powerToWeight.value.trim(),
      "Привод": form.elements.drive.value.trim(),
      "Тип двигателя": form.elements.engineType.value.trim(),
      "Модель двигателя": form.elements.engineModel.value.trim(),
      "Объем, см3": form.elements.engineVolume.value.trim(),
      "Крутящий момент, Нм": form.elements.torque.value.trim()
    };
    const response = await fetch("/api/account/" + encodeURIComponent(accountUser.id) + "/vehicles", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: form.elements.name.value.trim(),
        image,
        specs,
        isTeamVehicle,
        teamType
      })
    });
    const data = await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || "Не удалось сохранить автомобиль.");
    accountUser = makeSessionUser(data.user);
    if (!accountUser.favoriteVehicleId && accountUser.vehicles[0]) accountUser.favoriteVehicleId = accountUser.vehicles[0].id;
    saveCurrentUser(accountUser);
    renderAccountVehicles(accountUser.vehicles);
    setAccountText("[data-account-car]", accountUser.vehicles.find((item) => item.id === accountUser.favoriteVehicleId)?.name || accountUser.vehicles[0]?.name);
    showSiteToast(isTeamVehicle ? "Командный автомобиль добавлен" : "Автомобиль добавлен");
    form.reset();
    closeAccountModal(vehicleModal);
  } catch (error) {
    showSiteToast(error.message || "Не удалось сохранить автомобиль", "error");
  } finally {
    if (submitButton) {
      submitButton.disabled = false;
      submitButton.textContent = "Добавить автомобиль";
    }
  }
});

document.querySelector("[data-account-settings-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!accountUser) return;
  const form = event.currentTarget;

  try {
    const avatarFile = form.elements.avatar.files[0];
    const avatar = avatarFile ? await readImageFile(avatarFile) : accountUser.avatar;

    const payload = {
      lastName: form.elements.lastName.value.trim(),
      firstName: form.elements.firstName.value.trim(),
      middleName: form.elements.middleName.value.trim(),
      email: form.elements.email.value.trim().toLowerCase(),
      phone: form.elements.phone.value.trim(),
      avatar,
      driverNumber: form.elements.driverNumber?.value.trim() || "",
      organizationName: accountUser.profile?.organizationName || "",
      organizationColor: accountUser.profile?.organizationColor || "#e10600",
      organizationLogo: accountUser.profile?.organizationLogo || "",
      organizationBanner: accountUser.profile?.organizationBanner || "",
      organizationMembers: accountUser.profile?.organizationMembers || [],
      racingTeamName: accountUser.profile?.racingTeamName || "",
      racingTeamColor: accountUser.profile?.racingTeamColor || "#e10600",
      racingTeamLogo: accountUser.profile?.racingTeamLogo || "",
      racingTeamBanner: accountUser.profile?.racingTeamBanner || "",
      racingTeamMembers: accountUser.profile?.racingTeamMembers || []
    };

    const response = await fetch(`/api/account/${encodeURIComponent(accountUser.id)}/profile`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    let data = null;
    try { data = await response.json(); } catch (error) { data = null; }
    if (!response.ok) throw new Error(data?.message || "Не удалось сохранить профиль в базе данных.");

    accountUser = makeSessionUser(data.user);
    saveCurrentUser(accountUser);
    const profile = accountUser.profile || {};
    const fullName = [profile.lastName, profile.firstName, profile.middleName].filter(Boolean).join(" ");
    setAccountText("[data-account-full-name]", fullName);
    setAccountText("[data-account-email]", accountUser.email);
    setAccountText("[data-account-last-name]", profile.lastName);
    setAccountText("[data-account-first-name]", profile.firstName);
    setAccountText("[data-account-middle-name]", profile.middleName);
    setAccountText("[data-account-phone]", profile.phone);
    setAccountText("[data-account-driver-number]", profile.driverNumber || "—");
    renderAccountAvatar(accountUser.avatar);

    const message = document.querySelector("[data-settings-message]");
    if (message) {
      message.textContent = "Изменения сохранены";
      message.hidden = false;
      setTimeout(() => { message.hidden = true; }, 3000);
    }
    showSiteToast("Данные профиля сохранены в базе данных");
  } catch (error) {
    showSiteToast(error.message || "Не удалось сохранить изменения", "error");
  }
});

document.querySelector("[data-account-logout]")?.addEventListener("click", () => {
  logoutCurrentUser();
});


function getUserInitials(user) {
  const profile = user?.profile || {};
  return [profile.firstName, profile.lastName].filter(Boolean).map((part) => part[0]).join("").toUpperCase() || (user?.login || "RM").slice(0, 2).toUpperCase();
}

async function logoutCurrentUser() {
  try {
    await fetch("/api/auth/logout", { method: "POST", credentials: "include" });
  } catch (error) {
    // Local cleanup still signs the user out when the API is temporarily unavailable.
  }
  localStorage.removeItem(currentUserStorageKey);
  window.location.href = "/pages/auth/login.html";
}

function renderHeaderProfileMenu() {
  const user = getCurrentUser();
  if (!user) return;
  const actions = document.querySelector(".header-actions");
  if (!actions || actions.querySelector("[data-header-profile-menu]")) return;
  const initials = getUserInitials(user);
  const profileName = user.login || user.profile?.firstName || "Профиль";
  const avatarStyle = user.avatar ? ' style="background-image:url(\'' + user.avatar + '\');color:transparent"' : "";
  const profileMeta = [user.email, user.role && user.role !== "Пользователь" ? user.role : "Участник"].filter(Boolean).join(" · ");
  const searchButton = actions.querySelector("[data-search-open]");
  const profileMenu = document.createElement("div");
  profileMenu.className = "header-profile-menu";
  profileMenu.dataset.headerProfileMenu = "";
  profileMenu.innerHTML = '<button class="header-avatar-link" type="button" data-header-profile-toggle aria-expanded="false"><span' + avatarStyle + '>' + initials + '</span></button><div class="header-profile-dropdown"><div class="header-profile-dropdown__head"><span' + avatarStyle + '>' + initials + '</span><div><strong>' + profileName + '</strong><small>' + profileMeta + '</small></div><i>⌃</i></div><a href="/pages/account/account.html"><b>●</b><span><strong>Мой профиль</strong><small>Статистика и заявки</small></span></a><a href="/pages/account/account.html#settings"><b>⚙</b><span><strong>Настройки</strong><small>Данные и аватар</small></span></a><button type="button" data-header-logout><b>↳</b><span><strong>Выйти</strong><small>Завершить сессию</small></span></button></div>';
  actions.querySelector('a.button[href="/pages/auth/login.html"]')?.remove();
  actions.querySelector(".account-header-user")?.remove();
  if (searchButton) searchButton.after(profileMenu);
  else actions.append(profileMenu);
  profileMenu.querySelector("[data-header-profile-toggle]").addEventListener("click", () => {
    const isOpen = profileMenu.classList.toggle("is-open");
    profileMenu.querySelector("[data-header-profile-toggle]").setAttribute("aria-expanded", String(isOpen));
  });
  profileMenu.querySelector("[data-header-logout]").addEventListener("click", logoutCurrentUser);
  translateElementTree(localStorage.getItem(siteLanguageStorageKey) || "ru");
}

renderHeaderProfileMenu();
function notificationDateLabel(value) {
  if (!value) return "Недавно";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Недавно";
  return new Intl.DateTimeFormat("ru-RU", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" }).format(date);
}

function userNotificationItems(user) {
  const profile = user?.profile || {};
  const invitations = (profile.teamInvitations || []).filter((item) => item.status === "Pending").map((item) => ({
    id: item.id,
    message: item.teamType === "Judge"
      ? (item.ownerName || "Организатор") + " приглашает вас судить событие «" + item.teamName + "»."
      : (item.ownerName || "Лидер команды") + " приглашает вас в команду «" + item.teamName + "».",
    createdAt: item.createdAt,
    isRead: Boolean(item.isRead),
    kind: "invite"
  }));
  const notifications = (profile.teamNotifications || []).map((item) => ({
    id: item.id,
    message: item.message,
    createdAt: item.createdAt,
    isRead: Boolean(item.isRead),
    kind: "info"
  }));
  return [...invitations, ...notifications].sort((left, right) => new Date(right.createdAt || 0) - new Date(left.createdAt || 0));
}

function ensureHeaderSearchButton(actions) {
  let searchButton = actions.querySelector("[data-search-open]");
  if (searchButton) return searchButton;
  searchButton = document.createElement("button");
  searchButton.className = "icon-button";
  searchButton.type = "button";
  searchButton.dataset.searchOpen = "";
  searchButton.setAttribute("aria-label", "Поиск");
  searchButton.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="6"></circle><path d="m16 16 5 5"></path></svg>';
  actions.prepend(searchButton);
  return searchButton;
}

function renderHeaderNotifications(user) {
  const actions = document.querySelector(".header-actions");
  if (!actions || !user) return;
  const searchButton = ensureHeaderSearchButton(actions);
  let menu = actions.querySelector("[data-header-notifications]");
  if (!menu) {
    menu = document.createElement("div");
    menu.className = "header-notifications";
    menu.dataset.headerNotifications = "";
    menu.innerHTML = '<button class="header-notifications__trigger" type="button" data-notification-toggle aria-label="Уведомления" aria-expanded="false"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9"></path><path d="M10 21h4"></path></svg><span data-notification-count hidden>0</span></button><div class="header-notifications__dropdown"><div class="header-notifications__head"><div><small>RaceManager</small><strong>Уведомления</strong></div><button type="button" data-notifications-read>Прочитать все</button></div><div class="header-notifications__list" data-header-notification-list></div><a class="header-notifications__all" href="/pages/account/account.html#team-notifications">Посмотреть все уведомления</a></div>';
    const profileMenu = actions.querySelector("[data-header-profile-menu]");
    if (profileMenu) profileMenu.before(menu);
    else searchButton.after(menu);

    menu.querySelector("[data-notification-toggle]").addEventListener("click", async () => {
      const isOpen = menu.classList.toggle("is-open");
      menu.querySelector("[data-notification-toggle]").setAttribute("aria-expanded", String(isOpen));
      if (isOpen) await markHeaderNotificationsRead();
    });
    menu.querySelector("[data-notifications-read]").addEventListener("click", markHeaderNotificationsRead);
  }

  const items = userNotificationItems(user);
  const unread = items.filter((item) => !item.isRead).length;
  const badge = menu.querySelector("[data-notification-count]");
  badge.textContent = unread > 99 ? "99+" : String(unread);
  badge.hidden = unread === 0;
  const list = menu.querySelector("[data-header-notification-list]");
  list.innerHTML = items.length ? items.map((item) =>
    '<a class="header-notification-item' + (!item.isRead ? ' is-unread' : '') + '" data-header-notification-id="' + escapeHtml(item.id) + '" href="/pages/account/account.html#team-notifications"><span class="header-notification-item__icon">' +
    (item.kind === "invite" ? "R" : "i") + '</span><span><strong>' + escapeHtml(item.message) + '</strong><small>' + notificationDateLabel(item.createdAt) + '</small></span></a>'
  ).join("") : '<div class="header-notifications__empty"><strong>Уведомлений пока нет</strong><span>Здесь появятся приглашения и ответы команды.</span></div>';

  list.querySelectorAll(".header-notification-item.is-unread").forEach((item) => {
    item.addEventListener("mouseenter", () => markHeaderNotificationRead(item.dataset.headerNotificationId), { once: true });
  });
}

async function applyNotificationReadResponse(response) {
  if (!response.ok) return false;
  const data = await response.json();
  const freshUser = makeSessionUser(data.user);
  const previousAccountUser = accountUser;
  if (accountPage) accountUser = freshUser;
  saveCurrentUser(freshUser);
  refreshTeamEditorsIfChanged(previousAccountUser, freshUser);
  renderHeaderNotifications(freshUser);
  renderTeamInvitationCenter(freshUser);
  return true;
}

async function markHeaderNotificationRead(notificationId) {
  const user = getCurrentUser();
  if (!user?.id || !notificationId) return;
  try {
    const response = await fetch("/api/account/" + encodeURIComponent(user.id) + "/notifications/" + encodeURIComponent(notificationId) + "/read", {
      method: "POST",
      credentials: "include"
    });
    await applyNotificationReadResponse(response);
  } catch (error) {
    // Фоновая синхронизация повторно загрузит состояние уведомлений.
  }
}

async function markHeaderNotificationsRead() {
  const user = getCurrentUser();
  if (!user) return;
  try {
    const response = await fetch("/api/account/" + encodeURIComponent(user.id) + "/notifications/read", {
      method: "POST",
      credentials: "include"
    });
    await applyNotificationReadResponse(response);
  } catch (error) {
    // Меню остаётся доступным даже при временной потере соединения.
  }
}

async function refreshHeaderNotifications() {
  const current = getCurrentUser();
  if (!current?.id) return;
  const previousUnread = userNotificationItems(current).filter((item) => !item.isRead).length;
  try {
    const response = await fetch("/api/users/" + encodeURIComponent(current.id), { cache: "no-store" });
    if (!response.ok) return;
    const freshUser = makeSessionUser(await response.json());
    const unread = userNotificationItems(freshUser).filter((item) => !item.isRead).length;
    if (accountPage) accountUser = freshUser;
    saveCurrentUser(freshUser);
    renderHeaderNotifications(freshUser);
    renderTeamInvitationCenter(freshUser);
    if (unread > previousUnread) showSiteToast("У вас новое уведомление");
  } catch (error) {
    // Фоновая синхронизация повторится автоматически.
  }
}

const headerNotificationUser = getCurrentUser();
if (headerNotificationUser) {
  renderHeaderNotifications(headerNotificationUser);
  refreshHeaderNotifications();
  setInterval(refreshHeaderNotifications, 10000);
}

document.addEventListener("click", (event) => {
  if (event.target.closest("[data-header-notifications]")) return;
  document.querySelectorAll("[data-header-notifications].is-open").forEach((menu) => {
    menu.classList.remove("is-open");
    menu.querySelector("[data-notification-toggle]")?.setAttribute("aria-expanded", "false");
  });
});

if (accountPage && window.location.hash === "#team-notifications") {
  switchAccountTab("profile");
  void markHeaderNotificationsRead();
  requestAnimationFrame(() => document.querySelector("[data-team-invitation-center]")?.scrollIntoView({ behavior: "smooth", block: "center" }));
}


document.addEventListener("click", (event) => {
  if (event.target.closest("[data-header-profile-menu]")) return;
  document.querySelectorAll("[data-header-profile-menu].is-open").forEach((menu) => menu.classList.remove("is-open"));
});

document.querySelectorAll(".calendar-event__primary").forEach((button) => {
  const action = button.textContent.trim().toLowerCase();
  const isRegistrationAction = action.includes("зарегистр") || action.includes("записаться");
  if (!isRegistrationAction) return;

  button.addEventListener("click", (event) => {
    event.preventDefault();
    const user = getCurrentUser();
    if (!user) {
      window.location.href = "/pages/auth/login.html";
      return;
    }

    const card = button.closest(".calendar-event");
    if (!card) return;
    const day = card.querySelector(".calendar-event__date span")?.textContent.trim() || "";
    const month = card.querySelector(".calendar-event__date small")?.textContent.trim() || "";
    const year = card.querySelector(".calendar-event__date em")?.textContent.trim() || "";
    const name = card.querySelector(".calendar-event__content h3")?.textContent.trim() || "Событие RaceManager";
    const meta = [...card.querySelectorAll(".calendar-event__meta span")].map((item) => item.textContent.trim());
    const label = card.querySelector(".calendar-event__label")?.textContent.trim() || "Событие";
    const application = { id: name.toLowerCase().replace(/\s+/g, "-") + "-" + year, date: `${day} ${month} ${year}`, name, location: meta[0] || "RaceManager", type: label, href: "calendar.html" };

    user.applications = user.applications || [];
    if (!user.applications.some((item) => item.id === application.id)) user.applications.push(application);
    saveCurrentUser(user);
    button.textContent = "Заявка подана";
    button.classList.add("is-registered");
  });
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape") return;
  document.querySelectorAll(".account-modal.is-open").forEach(closeAccountModal);
});

const dragRegisterModal = document.querySelector(".drag-register-modal");
const dragRegisterForm = document.querySelector("[data-drag-register-form]");
const dragParticipantsBody = document.querySelector("[data-drag-participants]");
let dragParticipantsCache = [];
const drag402Stage2Application = {
  id: "drag402-stage-2",
  eventKey: "drag402-stage-2",
  name: "Drag402 Кубок Городов · 2 этап",
  date: "20–21 июня 2026",
  location: "Аэропорт Могилев",
  type: "Дрэг-Рейсинг",
  href: "/pages/championships/Drag402Stage2.html",
};

function getDragParticipants() {
  return dragParticipantsCache;
}

function saveDragParticipants(participants) {
  dragParticipantsCache = participants;
}

function getDragDatabaseEvent() {
  return getOrganizerEvents().find((event) =>
    event.name.toLowerCase().includes("drag402") ||
    (event.discipline.toLowerCase().includes("дрэг") && event.track.toLowerCase().includes("могил")));
}

function normalizePersonName(value = "") {
  return value.toLowerCase().replace(/ё/g, "е").replace(/[^a-zа-я0-9]+/gi, " ").trim().replace(/\s+/g, " ");
}


const racePointsByPosition = [25, 18, 15, 12, 10, 8, 6, 4, 2, 1];

function parseRaceSeconds(value) {
  if (value === null || value === undefined) return null;
  const normalized = String(value).replace(',', '.').trim();
  if (!normalized) return null;
  const lapMatch = normalized.match(/^(?:(\d{1,2}):)?(\d{1,2})(?:\.(\d{1,3}))?$/);
  if (lapMatch && lapMatch[1] !== undefined) {
    const minutes = Number(lapMatch[1]);
    const seconds = Number(lapMatch[2]);
    const milliseconds = Number((lapMatch[3] || '').padEnd(3, '0') || 0);
    return minutes * 60 + seconds + milliseconds / 1000;
  }
  const seconds = Number(normalized);
  return Number.isFinite(seconds) ? seconds : null;
}

function formatRaceSeconds(value) {
  const seconds = parseRaceSeconds(value);
  return seconds === null ? '—' : seconds.toFixed(3).replace(/\.000$/, '.0');
}

function isElectricCarName(carName = '') {
  return /tesla|zeekr|electro|electric|ev|электро/i.test(String(carName));
}

function disciplineKey(value = '') {
  const text = String(value).toLowerCase();
  if (text.includes('drag') || text.includes('дрэг') || text.includes('драг')) return 'drag';
  if (text.includes('time') || text.includes('тайм')) return 'timeAttack';
  return 'drift';
}

function eventNeedsQualificationTime(eventOrDiscipline) {
  const value = typeof eventOrDiscipline === 'string' ? eventOrDiscipline : `${eventOrDiscipline?.discipline || ''} ${eventOrDiscipline?.type || ''}`;
  const key = disciplineKey(value);
  return key === 'drag' || key === 'timeAttack';
}

function resolveClassMode(eventOrDiscipline) {
  const value = typeof eventOrDiscipline === 'string' ? eventOrDiscipline : `${eventOrDiscipline?.classMode || ''} ${eventOrDiscipline?.mode || ''}`;
  return /handicap/i.test(value) ? 'StandardDragHandicap' : 'StandardDrag';
}

function classifyParticipant(discipline, secondsValue, carName = '', mode = '') {
  const seconds = parseRaceSeconds(secondsValue);
  if (seconds === null) return 'Не указан';
  const key = disciplineKey(discipline);
  const electric = isElectricCarName(carName);

  if (key === 'drag') {
    if (mode === 'StandardDragHandicap') {
      if (electric && seconds >= 9.5 && seconds <= 15.0) return 'Electro Handicap';
      if (seconds >= 14.0 && seconds <= 14.999) return 'Club Handicap';
      if (seconds >= 11.0 && seconds <= 12.999) return 'Sport Handicap';
      if (seconds >= 9.5 && seconds <= 10.999) return 'Pro Handicap';
      if (seconds >= 13.0 && seconds <= 15.0) return 'Street Handicap';
      return 'Вне класса';
    }
    if (electric && seconds >= 9.5 && seconds <= 9.999) return 'Electro';
    if (seconds >= 14.0 && seconds <= 14.999) return 'Club';
    if (seconds >= 13.0 && seconds <= 13.999) return 'Street';
    if (seconds >= 12.0 && seconds <= 12.999) return 'Sport';
    if (seconds >= 10.0 && seconds <= 10.999) return 'Pro';
    return 'Вне класса';
  }

  if (key === 'timeAttack') {
    if (seconds >= 14.0 && seconds <= 15.5) return 'Stock';
    if (seconds >= 13.0 && seconds <= 13.999) return 'Street';
    if (seconds >= 12.0 && seconds <= 12.999) return 'Sport';
    if (seconds >= 11.0 && seconds <= 11.999) return 'Charged';
    if (seconds >= 10.0 && seconds <= 10.999) return 'Pro';
    if (seconds >= 9.5 && seconds <= 9.999) return 'Unlim';
    return 'Вне класса';
  }

  return 'Не требуется';
}

function pointsForPosition(position) {
  return racePointsByPosition[position - 1] || 0;
}

function secondsToMs(value) {
  const seconds = parseRaceSeconds(value);
  return seconds === null ? null : Math.round(seconds * 1000);
}

function msToSeconds(ms) {
  return Number.isFinite(ms) ? (ms / 1000).toFixed(3).replace(/\.000$/, '.0') : '—';
}

function msToLapTime(ms) {
  if (!Number.isFinite(Number(ms))) return '—';
  const totalMs = Math.max(0, Math.round(Number(ms)));
  const minutes = Math.floor(totalMs / 60000);
  const seconds = Math.floor((totalMs % 60000) / 1000);
  const milliseconds = totalMs % 1000;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}.${String(milliseconds).padStart(3, '0')}`;
}

function userFullName(user) {
  const profile = user?.profile || {};
  return [profile.lastName, profile.firstName, profile.middleName].filter(Boolean).join(" ");
}


function userRacingTeamName(user) {
  const profile = user?.profile || {};
  if (profile.racingTeamName) return profile.racingTeamName;
  return profile.teamMemberships?.find((item) => item.teamType === "Racing")?.teamName || "Нету";
}

function isCurrentUserParticipant(participant) {
  const current = getCurrentUser();
  if (!current || !participant) return false;
  const currentEmail = current.email?.toLowerCase();
  const participantEmail = participant.email?.toLowerCase();
  if (currentEmail && participantEmail && currentEmail === participantEmail) return true;
  const currentName = normalizePersonName(userFullName(current));
  const participantName = normalizePersonName(participant.fullName);
  return Boolean(currentName && participantName && currentName === participantName);
}

function renderDragParticipants() {
  if (!dragParticipantsBody) return;
  const participants = getDragParticipants();
  if (!participants.length) {
    dragParticipantsBody.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="11">Пока нет зарегистрированных участников.</td></tr>';
    return;
  }
  dragParticipantsBody.innerHTML = participants.map((pilot, index) => {
    const status = pilot.status || "Зарегистрирован";
    const isDeclined = status === "Отклонил участие";
    const canDecline = isCurrentUserParticipant(pilot);
    const declineAction = isDeclined || !canDecline ? "—" : '<button class="button button--red button--small" type="button" data-drag-decline="' + pilot.id + '">Сняться</button>';
    return '<tr class="' + (isDeclined ? 'is-declined' : '') + '">' +
      '<td>' + (index + 1) + '</td>' +
      '<td>' + (pilot.driverNumber || '—') + '</td>' +
      '<td>' + pilot.fullName + '</td>' +
      '<td>' + (pilot.teamName || 'Нету') + '</td>' +
      '<td>' + pilot.email + '</td>' +
      '<td>' + pilot.phone + '</td>' +
      '<td>' + pilot.car + '</td>' +
      '<td>' + formatRaceSeconds(pilot.qualificationTime) + '</td>' +
      '<td>' + (pilot.className || 'Не указан') + '</td>' +
      '<td><span class="drag-entry-status ' + (isDeclined ? 'drag-entry-status--declined' : '') + '">' + status + '</span></td>' +
      '<td>' + declineAction + '</td>' +
    '</tr>';
  }).join("");
}

function populateDragRegisterCars() {
  const select = document.querySelector("[data-register-car-select]");
  if (!select) return;
  const user = getCurrentUser();
  const vehicles = user?.vehicles || [];
  select.innerHTML = '<option value="">Выбрать добавленный автомобиль</option>' + vehicles.map((vehicle) => `<option value="${vehicle.id}">${vehicle.name} · ${vehicleValue(vehicle.power, "л.с.")}</option>`).join("");
  select.disabled = !vehicles.length;
}

function getSelectedRegistrationVehicle(form) {
  const user = getCurrentUser();
  const selectedId = form.elements.savedCar?.value;
  return (user?.vehicles || []).find((vehicle) => vehicle.id === selectedId) || null;
}

function openDragRegisterModal() {
  if (!dragRegisterModal) return;
  populateDragRegisterCars();
  const user = getCurrentUser();
  if (user) {
    const fullName = userFullName(user);
    if (fullName && dragRegisterForm?.elements.fullName) dragRegisterForm.elements.fullName.value = fullName;
    if (user.email && dragRegisterForm?.elements.email) dragRegisterForm.elements.email.value = user.email;
    if (user.profile?.phone && dragRegisterForm?.elements.phone) dragRegisterForm.elements.phone.value = user.profile.phone;
  }
  dragRegisterModal.classList.add("is-open");
  dragRegisterModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
  const dialog = dragRegisterModal.querySelector(".drag-register-modal__dialog");
  if (dialog) dialog.scrollTop = 0;
  requestAnimationFrame(() => centerVisibleModal(dragRegisterModal));
  dragRegisterModal.querySelector("input")?.focus();
}

function closeDragRegisterModal() {
  if (!dragRegisterModal) return;
  dragRegisterModal.classList.remove("is-open");
  dragRegisterModal.setAttribute("aria-hidden", "true");
  document.body.classList.remove("modal-open");
}

renderDragParticipants();

document.querySelector("[data-drag-register-open]")?.addEventListener("click", openDragRegisterModal);
document.querySelectorAll("[data-drag-register-close]").forEach((button) => {
  button.addEventListener("click", closeDragRegisterModal);
});

dragRegisterForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const selectedVehicle = getSelectedRegistrationVehicle(form);
  const manualCar = form.elements.car.value.trim();
  const carName = selectedVehicle?.name || manualCar;
  if (!carName) {
    showSiteToast("Выберите автомобиль из профиля или введите его вручную", "error");
    return;
  }
  const qualificationTime = parseRaceSeconds(form.elements.qualificationTime?.value);
  if (qualificationTime === null) {
    showSiteToast("Укажите время 1/4 мили для определения класса", "error");
    return;
  }
  const classMode = "StandardDragHandicap";
  const className = classifyParticipant("Дрэг-рейсинг", qualificationTime, carName, classMode);
  const currentUser = getCurrentUser();
  const participant = {
    id: "drag402-" + Date.now(),
    fullName: form.elements.fullName.value.trim(),
    email: form.elements.email.value.trim().toLowerCase(),
    phone: form.elements.phone.value.trim(),
    driverNumber: currentUser?.profile?.driverNumber || "",
    teamName: userRacingTeamName(currentUser),
    car: carName,
    qualificationTime,
    className,
    classMode,
    carPower: selectedVehicle?.power || "",
    carId: selectedVehicle?.id || "",
    status: "Зарегистрирован",
  };
  const databaseEvent = getDragDatabaseEvent();
  if (!databaseEvent) { showSiteToast("Событие Drag402 не найдено в базе данных", "error"); return; }
  try {
    const data = await syncParticipantToApi(databaseEvent, participant);
    saveDragParticipants((data.event?.participants || []).map(participantFromApi));
  } catch (error) {
    showSiteToast(error.message || "Заявка не сохранена", "error");
    return;
  }
  renderDragParticipants();
  form.reset();
  closeDragRegisterModal();
  showSiteToast("Заявка на Drag402 отправлена");
});

dragParticipantsBody?.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-drag-decline]");
  if (!button) return;
  const participantId = button.dataset.dragDecline;
  const participants = getDragParticipants();
  const participant = participants.find((item) => item.id === participantId);
  if (!participant) return;
  if (!isCurrentUserParticipant(participant)) {
    showSiteToast("Можно снять только свою заявку", "error");
    return;
  }
  const databaseEvent = getDragDatabaseEvent();
  if (!databaseEvent) return;
  const response = await fetch(`/api/events/${encodeURIComponent(databaseEvent.id)}/registrations/${encodeURIComponent(participant.id)}/cancel`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: getCurrentUser()?.id || "", email: participant.email })
  });
  if (!response.ok) { showSiteToast("Не удалось снять заявку", "error"); return; }
  const data = await response.json();
  saveDragParticipants((data.event?.participants || []).map(participantFromApi));
  renderDragParticipants();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && dragRegisterModal?.classList.contains("is-open")) closeDragRegisterModal();
});

let organizerEventsCache = [];
const createdEventParticipantsCache = new Map();
const createdEventResultsCache = new Map();
const startListCache = new Map();

function getOrganizerEvents() {
  return organizerEventsCache;
}

function saveOrganizerEvents(events) {
  organizerEventsCache = events;
}

function eventFromApi(event) {
  return {
    id: event.id,
    apiId: event.id,
    championshipId: event.championshipId || "",
    organizerId: event.organizerUserId,
    type: event.type,
    name: event.title,
    discipline: event.discipline,
    participants: event.participantLimit || 0,
    track: event.track,
    distance: event.laps || event.distance || "",
    distanceLabel: event.laps ? "кругов" : "м",
    eventDate: event.date,
    trackConfig: event.trackConfigImage || "",
    eventImage: event.bannerImage || "",
    calendarImage: event.calendarBannerImage || "",
    organizationName: event.organizerName || "",
    organizationColor: event.organizerColor || "#e10600",
    organizationLogo: event.organizerLogo || "",
    registrationStatus: event.registrationStatus || "Регистрация закрыта",
    status: event.registrationStatus === "Завершено" ? "Завершено" : "Активно",
    intro: event.intro || "",
    stages: (event.stages || []).map((stage) => ({ ...stage, name: stage.title })),
    standings: { pilots: event.personalStandings || [], teams: event.teamStandings || [] }
  };
}

function participantFromApi(participant) {
  return {
    id: participant.id,
    apiId: participant.id,
    userId: participant.userId,
    fullName: participant.fullName,
    email: participant.email,
    phone: participant.phone || "",
    car: participant.car || "",
    teamName: participant.teamName || "Нету",
    driverNumber: participant.driverNumber || "",
    qualificationTime: participant.qualificationTimeSeconds,
    className: participant.className || "",
    status: participant.status || "Зарегистрирован"
  };
}

function parsePenaltyMs(value) {
  const parsed = raceInputToMs(value);
  if (parsed !== null) return Math.max(0, parsed);
  const text = String(value || "").replace(",", ".");
  const match = text.match(/-?\d+(?:\.\d+)?/);
  if (!match) return 0;
  return Math.max(0, Math.round(Number(match[0]) * 1000));
}
function raceInputToMs(value) {
  const seconds = parseRaceSeconds(value);
  return seconds === null ? null : secondsToMs(seconds);
}

function calculateJudgeFormTimes(form) {
  const laps = [form.elements.lap1, form.elements.lap2, form.elements.lap3]
    .map((input) => raceInputToMs(input?.value))
    .filter((value) => Number.isFinite(value) && value > 0);
  const bestLapMs = laps.length ? Math.min(...laps) : raceInputToMs(form.elements.bestLap?.value);
  const penaltyMs = parsePenaltyMs(form.elements.penalty?.value);
  if (bestLapMs) {
    form.elements.bestLap.value = msToLapTime(bestLapMs);
    form.elements.lapTime.value = msToLapTime(bestLapMs + penaltyMs);
  }
  return { bestLapMs, penaltyMs, finalTimeMs: bestLapMs ? bestLapMs + penaltyMs : raceInputToMs(form.elements.lapTime?.value) };
}


function resultFromApi(result) {
  const finalTimeMs = result.finalTimeMs ?? secondsToMs(parseRaceSeconds(result.lapTime) || 0);
  const bestLapMs = result.bestLapMs ?? result.bestLapTimeMs ?? (result.bestLap ? secondsToMs(parseRaceSeconds(result.bestLap) || 0) : null);
  return {
    id: result.id,
    participantId: result.participantId,
    fullName: result.driverName,
    driverNumber: result.driverNumber || "",
    car: result.carName || "",
    className: result.className || "",
    lap1Ms: result.lap1Ms,
    lap2Ms: result.lap2Ms,
    lap3Ms: result.lap3Ms,
    bestLapMs,
    penaltyMs: result.penaltyMs || 0,
    penalty: (result.penalties || []).map((penalty) => penalty.reason).join(", "),
    finalTimeMs,
    position: result.position,
    points: result.points,
    status: result.status
  };
}

async function loadDatabaseEventState() {
  const [eventsResponse, resultsResponse] = await Promise.all([
    fetch("/api/events", { cache: "no-store" }),
    fetch("/api/results", { cache: "no-store" })
  ]);
  if (!eventsResponse.ok || !resultsResponse.ok) throw new Error("Не удалось загрузить данные соревнований из БД.");
  const apiEvents = await eventsResponse.json();
  const apiResults = await resultsResponse.json();
  organizerEventsCache = apiEvents.map(eventFromApi);
  createdEventParticipantsCache.clear();
  apiEvents.forEach((event) => createdEventParticipantsCache.set(event.id, (event.participants || []).map(participantFromApi)));
  const dragEvent = getDragDatabaseEvent();
  dragParticipantsCache = dragEvent ? getCreatedEventParticipants(dragEvent.id) : [];
  createdEventResultsCache.clear();
  judgeResultsCache = {};
  apiResults.forEach((result) => {
    const rows = createdEventResultsCache.get(result.eventId) || [];
    rows.push(resultFromApi(result));
    createdEventResultsCache.set(result.eventId, rows);
    const judgeRows = judgeResultsCache[result.eventId] || [];
    judgeRows.push({
      id: result.id,
      pilotName: result.driverName,
      position: result.position,
      lapTime: result.lapTime,
      bestLap: result.bestLap,
      lap1Ms: result.lap1Ms,
      lap2Ms: result.lap2Ms,
      lap3Ms: result.lap3Ms,
      penaltyMs: result.penaltyMs || 0,
      penalty: (result.penalties || []).map((penalty) => penalty.reason).join(", "),
      status: result.status,
      points: result.points,
      updatedAt: result.updatedAtUtc
    });
    judgeResultsCache[result.eventId] = judgeRows;
  });
}


function eventRegistrationStatusValue(event) {
  return event.registrationStatus || "Регистрация открыта";
}

function isCreatedEventCompleted(event) {
  return event?.status === "Завершено" || eventRegistrationStatusValue(event) === "Завершено";
}

function isEventRegistrationOpen(event) {
  return eventRegistrationStatusValue(event) === "Регистрация открыта";
}

function createEventApiPayload(event) {
  return {
    organizerUserId: event.organizerId || getCurrentUser()?.id || "",
    type: event.type || "Трек-день",
    title: event.name || "Событие RaceManager",
    discipline: event.discipline || "Дрифт",
    participantLimit: Number(event.participants || 0),
    track: event.track || "RaceManager",
    distance: event.distance ? String(event.distance) : "",
    laps: event.distanceLabel === "кругов" ? Number(event.distance || 0) || null : null,
    trackConfigImage: event.trackConfig || "",
    bannerImage: event.eventImage || "",
    calendarBannerImage: event.calendarImage || "",
    organizerName: event.organizationName || getCurrentUser()?.profile?.organizationName || "",
    organizerColor: event.organizationColor || getCurrentUser()?.profile?.organizationColor || "#e10600",
    organizerLogo: event.organizationLogo || getCurrentUser()?.profile?.organizationLogo || "",
    championshipId: event.championshipId || null,
    date: event.eventDate || "",
    registrationStatus: eventRegistrationStatusValue(event),
    intro: event.intro || "",
    stages: (event.stages || []).map((stage) => ({
      title: stage.name || stage.title || "Этап",
      date: stage.date || "",
      intro: stage.intro || "",
      registrationStatus: stage.registrationStatus || "Скоро",
      bannerImage: stage.bannerImage || ""
    })),
    personalStandings: [],
    teamStandings: []
  };
}

async function createChampionshipForEvent(event) {
  const response = await fetch("/api/championships", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      name: event.name,
      discipline: event.discipline,
      seasonYear: Number(String(event.eventDate || "").slice(0, 4)) || new Date().getFullYear(),
      description: event.intro || "",
      bannerUrl: event.eventImage || "",
      regulationFileUrl: "",
      status: "Active"
    })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось создать чемпионат в базе данных.");
  return data.championship;
}

async function loadChampionshipStandings(event) {
  if (!event?.championshipId) return;
  const response = await fetch(`/api/championships/${encodeURIComponent(event.championshipId)}`, { cache: "no-store" });
  if (!response.ok) return;
  const championship = await response.json();
  event.standings = {
    pilots: (championship.driverStandings || []).map((row) => ({ name: row.name, total: row.points, position: row.position })),
    teams: (championship.teamStandings || []).map((row) => ({ name: row.team || row.name, total: row.points, position: row.position }))
  };
}

async function syncEventToApi(event, mode = "create") {
  const payload = createEventApiPayload(event);
  const path = mode === "update" ? `/api/events/${encodeURIComponent(event.apiId || event.id)}` : "/api/events";
  const method = mode === "update" ? "PUT" : "POST";
  const response = await fetch(path, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  let data = null;
  try { data = await response.json(); } catch (error) { data = null; }
  if (!response.ok) throw new Error(data?.message || "Не удалось сохранить событие в базе данных.");
  if (!data?.event?.id) throw new Error("Сервер не вернул идентификатор сохраненного события.");
  return data.event;
}


async function syncParticipantToApi(event, participant) {
  const apiId = event.apiId || event.id;
  const response = await fetch(`/api/events/${encodeURIComponent(apiId)}/registrations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      userId: getCurrentUser()?.id || "",
      fullName: participant.fullName,
      email: participant.email,
      phone: participant.phone,
      car: participant.car,
      qualificationTimeSeconds: participant.qualificationTime ?? null,
      className: participant.className || "",
      teamName: participant.teamName || userRacingTeamName(getCurrentUser())
    })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось сохранить заявку в базе данных.");
  return data;
}

async function syncCreatedEventResultsToApi(event, results) {
  for (const row of results) {
    const response = await fetch("/api/results", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        eventId: event.apiId || event.id,
        stageId: null,
        participantId: row.participantId || null,
        driverName: row.fullName,
        position: row.position,
        lapTime: msToSeconds(row.finalTimeMs),
        bestLap: msToSeconds(row.bestLapMs),
        points: row.points,
        status: row.status || "Финишировал",
        judgeUserId: null,
        lap1Ms: row.lap1Ms,
        lap2Ms: row.lap2Ms,
        lap3Ms: row.lap3Ms,
        penaltyMs: row.penaltyMs || 0,
        finalTimeMs: row.finalTimeMs,
        className: row.className || "",
        carName: row.car || "",
        driverNumber: row.driverNumber || ""
      })
    });
    if (!response.ok) {
      const data = await response.json().catch(() => null);
      throw new Error(data?.message || `Не удалось сохранить результат пилота «${row.fullName}».`);
    }
  }
}

async function loadCreatedEventResultsFromApi(eventId) {
  const response = await fetch(`/api/results?eventId=${encodeURIComponent(eventId)}&sort=time`, {
    credentials: "include",
    cache: "no-store"
  });
  if (!response.ok) throw new Error("Не удалось подтвердить сохранение результатов в базе данных.");
  const results = (await response.json()).map(resultFromApi);
  saveCreatedEventResults(eventId, results);
  return results;
}

function escapeSpreadsheetCell(value) {
  return String(value ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function downloadTextFile(filename, mimeType, content) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function createdEventResultRowsForExport(event) {
  const results = getCreatedEventResults(event.id);
  return results.map((row) => [
    row.position,
    row.driverNumber || "—",
    row.fullName,
    row.car || "—",
    row.className || "—",
    msToLapTime(row.bestLapMs),
    row.penaltyMs ? msToLapTime(row.penaltyMs) : "—",
    msToLapTime(row.finalTimeMs),
    row.points,
    row.status || "Финишировал"
  ]);
}

async function downloadCreatedEventReportFromApi(event, format, reportType = "results") {
  const apiFormat = format === "word" ? "docx" : (format === "excel" ? "xlsx" : format);
  const suffix = reportType === "start-list" ? "/start-list" : "";
  const response = await fetch(`/api/reports/events/${encodeURIComponent(event.id)}${suffix}?format=${encodeURIComponent(apiFormat)}`, { credentials: "include" });
  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.message || "Не удалось сформировать отчет.");
  }
  const blob = await response.blob();
  const extension = apiFormat === "docx" ? "docx" : (apiFormat === "xlsx" ? "xlsx" : apiFormat);
  const safeName = (event.name || "RaceManager-results").replace(/[^\p{L}\p{N}]+/gu, "-").replace(/^-|-$/g, "") || "RaceManager-results";
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${safeName}-${reportType}.${extension}`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

async function exportCreatedEventResults(event, format) {
  event = getEventById(event.id) || event;
  const mode = createdEventTableMode(event);
  if (mode === "registrations") {
    showSiteToast("Сначала сформируйте стартовый список", "error");
    return;
  }
  if (mode === "start-list") {
    if (!(startListCache.get(event.id) || []).length) {
      showSiteToast("Стартовый список пока пуст", "error");
      return;
    }
    try {
      await downloadCreatedEventReportFromApi(event, format, "start-list");
    } catch (error) {
      showSiteToast(error.message || "Не удалось скачать стартовый список", "error");
    }
    return;
  }
  const rows = createdEventResultRowsForExport(event);
  if (!rows.length) {
    showSiteToast("Сначала завершите заезд, чтобы сформировать результаты", "error");
    return;
  }
  if (["txt", "word", "pdf", "excel"].includes(format)) {
    try {
      await downloadCreatedEventReportFromApi(event, format);
    } catch (error) {
      showSiteToast(error.message || "Не удалось скачать отчет", "error");
    }
    return;
  }
  const headers = ["Позиция", "№ пилота", "ФИО", "Автомобиль", "Класс", "Лучший круг", "Штраф", "Итог", "Очки", "Статус"];
  const safeName = (event.name || "RaceManager-results").replace(/[^\p{L}\p{N}]+/gu, "-").replace(/^-|-$/g, "") || "RaceManager-results";
  const table = `<table><thead><tr>${headers.map((cell) => `<th>${escapeSpreadsheetCell(cell)}</th>`).join("")}</tr></thead><tbody>${rows.map((row) => `<tr>${row.map((cell) => `<td>${escapeSpreadsheetCell(cell)}</td>`).join("")}</tr>`).join("")}</tbody></table>`;
  const excel = `<!doctype html><html><head><meta charset="utf-8"></head><body>${table}</body></html>`;
  downloadTextFile(`${safeName}-results.xls`, "application/vnd.ms-excel;charset=utf-8", excel);
}

function eventTypeLabel(type) {
  return type === "Чемпионат" ? "чемпионат" : type.toLowerCase();
}

function setupRoleUi(user) {
  if (!user) return;
  const isSupportAdmin = ["Технический админ", "Технический администратор"].includes(user.role);
  const isPrivileged = user.role === "Организатор" || user.role === "Судья" || isSupportAdmin;
  document.querySelectorAll("[data-account-role-badge], [data-account-profile-role-badge]").forEach((badge) => {
    badge.hidden = !isPrivileged;
    badge.textContent = user.role || "Пользователь";
    badge.classList.toggle("account-role-badge--judge", user.role === "Судья");
    badge.classList.toggle("account-role-badge--organizer", user.role === "Организатор");
    badge.classList.toggle("account-role-badge--support", isSupportAdmin);
  });
  document.querySelectorAll("[data-organizer-only]").forEach((element) => {
    element.hidden = user.role !== "Организатор";
  });
  document.querySelectorAll("[data-judge-only]").forEach((element) => {
    element.hidden = user.role !== "Судья";
  });
  document.querySelectorAll("[data-support-admin-only]").forEach((element) => {
    if (element.matches("[data-account-view]")) {
      if (!isSupportAdmin) element.hidden = true;
      return;
    }
    element.hidden = !isSupportAdmin;
  });
}

setupRoleUi(getCurrentUser());

const createEventForm = document.querySelector("[data-create-event-form]");
const createEventPage = document.querySelector(".create-event-page");

if (createEventPage) {
  const user = getCurrentUser();
  if (!user || user.role !== "Организатор") window.location.href = "/pages/account/account.html";
}


function renderSavedEventTeam(profile = {}) {
  const name = document.querySelector("[data-create-team-name]");
  const banner = document.querySelector("[data-create-team-banner]");
  const logo = document.querySelector("[data-create-team-logo]");
  if (name) name.textContent = profile.organizationName || "Команда не настроена";
  if (banner) {
    banner.style.setProperty("--organization-color", profile.organizationColor || "#e10600");
    banner.style.backgroundImage = profile.organizationBanner ? 'linear-gradient(90deg, rgba(7,8,12,.9), rgba(7,8,12,.28)), url("' + profile.organizationBanner + '")' : "";
  }
  if (logo) logo.innerHTML = profile.organizationLogo ? '<img src="' + profile.organizationLogo + '" alt="Логотип команды">' : "<span>RM</span>";
}

function updateEventTeamMode() {
  const isCustom = document.querySelector("[data-event-team-mode]")?.value === "custom";
  const savedPreview = document.querySelector("[data-existing-team-preview]");
  const customFields = document.querySelector("[data-custom-team-fields]");
  if (savedPreview) savedPreview.hidden = isCustom;
  if (customFields) customFields.hidden = !isCustom;
}

if (createEventPage) renderSavedEventTeam(getCurrentUser()?.profile || {});
document.querySelector("[data-event-team-mode]")?.addEventListener("change", updateEventTeamMode);
updateEventTeamMode();

document.querySelectorAll("[data-create-tab]").forEach((tab) => {
  tab.addEventListener("click", () => {
    const selected = tab.dataset.createTab;
    document.querySelectorAll("[data-create-tab]").forEach((button) => button.classList.toggle("is-active", button === tab));
    document.querySelectorAll("[data-create-view]").forEach((view) => {
      view.hidden = view.dataset.createView !== selected;
      view.classList.toggle("is-active", view.dataset.createView === selected);
    });
  });
});


function syncStageImageFields() {
  const section = document.querySelector("[data-stage-image-section]");
  const list = document.querySelector("[data-stage-image-list]");
  if (!section || !list) return;
  const isChampionship = document.querySelector("[data-event-type]")?.value === "Чемпионат";
  section.hidden = !isChampionship;
  const count = isChampionship ? document.querySelectorAll("[data-stage-list] .create-event-stage-row").length : 0;
  while (list.children.length < count) {
    const index = list.children.length;
    const label = document.createElement("label");
    label.innerHTML = `<span>Изображение этапа ${index + 1}</span><input type="file" data-stage-image="${index}" accept="image/png,image/jpeg">`;
    list.append(label);
  }
  while (list.children.length > count) list.lastElementChild.remove();
}

function updateCreateEventFields() {
  const type = document.querySelector("[data-event-type]")?.value || "Чемпионат";
  const discipline = document.querySelector("[data-event-discipline]")?.value || "Дрифт";
  const stages = document.querySelector("[data-championship-stages]");
  const submitLabel = document.querySelector("[data-create-submit-label]");
  const distanceLabel = document.querySelector("[data-discipline-distance-label]");
  if (stages) stages.hidden = type !== "Чемпионат";
  if (submitLabel) submitLabel.textContent = eventTypeLabel(type);
  if (distanceLabel) distanceLabel.textContent = discipline === "Тайм-Аттак" ? "Количество кругов" : "Дистанция, м";
  syncStageImageFields();
}

document.querySelector("[data-event-type]")?.addEventListener("change", updateCreateEventFields);
document.querySelector("[data-event-discipline]")?.addEventListener("change", updateCreateEventFields);
updateCreateEventFields();

document.querySelector("[data-add-stage]")?.addEventListener("click", () => {
  const list = document.querySelector("[data-stage-list]");
  if (!list) return;
  const row = document.createElement("div");
  row.className = "create-event-stage-row";
  row.innerHTML = '<label><span>Название этапа</span><input type="text" name="stageName" placeholder="Следующий этап"></label><label><span>Дата этапа</span><input type="date" name="stageDate"></label><label class="create-event-stage-row__wide"><span>Вводная информация</span><textarea name="stageIntro" placeholder="Краткое описание этапа, место проведения, особенности регистрации"></textarea></label><label><span>Статус регистрации</span><select name="stageRegistrationStatus"><option>Регистрация открыта</option><option>Скоро</option><option>Регистрация закрыта</option><option>Лист ожидания</option></select></label>';
  list.append(row);
  syncStageImageFields();
});

async function readOptionalImage(input) {
  const file = input?.files?.[0];
  return file ? await readImageFile(file, 1400) : "";
}

createEventForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const user = getCurrentUser();
  const stageNames = [...form.querySelectorAll('input[name="stageName"]')].map((input) => input.value.trim());
  const stageDates = [...form.querySelectorAll('input[name="stageDate"]')].map((input) => input.value);
  const stageIntros = [...form.querySelectorAll('[name="stageIntro"]')].map((input) => input.value.trim());
  const stageStatuses = [...form.querySelectorAll('[name="stageRegistrationStatus"]')].map((input) => input.value);
  const isChampionship = form.elements.eventType.value === "Чемпионат";
  const stages = isChampionship ? stageNames.map((name, index) => ({ name: name || `${index + 1} этап`, date: stageDates[index] || "", intro: stageIntros[index] || "", registrationStatus: stageStatuses[index] || "Скоро" })) : [];
  const stageImages = await Promise.all([...form.querySelectorAll("[data-stage-image]")].map((input) => readOptionalImage(input)));
  stages.forEach((stage, index) => { stage.bannerImage = stageImages[index] || ""; });
  const eventImage = await readOptionalImage(form.elements.eventImage);
  let calendarImage = await readOptionalImage(form.elements.calendarImage);
  const teamMode = form.elements.teamMode?.value || "saved";
  let organizationName = user?.profile?.organizationName || user?.profile?.racingTeamName || user?.login || "Организатор";
  let organizationColor = user?.profile?.organizationName
    ? (user.profile.organizationColor || "#e10600")
    : (user?.profile?.racingTeamColor || "#e10600");
  let organizationLogo = user?.profile?.organizationName
    ? (user.profile.organizationLogo || "")
    : (user?.profile?.racingTeamLogo || "");
  let organizationBanner = user?.profile?.organizationName
    ? (user.profile.organizationBanner || "")
    : (user?.profile?.racingTeamBanner || "");
  if (teamMode === "custom") {
    organizationName = form.elements.customOrganizationName?.value.trim() || user?.login || "Организатор";
    organizationColor = form.elements.customOrganizationColor?.value || "#e10600";
    organizationLogo = await readOptionalImage(form.elements.customOrganizationLogo);
    organizationBanner = await readOptionalImage(form.elements.customOrganizationBanner);
  }
  calendarImage = calendarImage || organizationBanner;
  const trackConfig = await readOptionalImage(form.elements.trackConfig);
  const createdEvent = {
    id: `event-${Date.now()}`,
    organizerId: user?.id || "organizer",
    organizer: user?.login || "Организатор",
    type: form.elements.eventType.value,
    name: form.elements.name.value.trim(),
    discipline: form.elements.discipline.value,
    participants: form.elements.participants.value,
    track: form.elements.track.value.trim(),
    distance: form.elements.distance.value,
    distanceLabel: form.elements.discipline.value === "Тайм-Аттак" ? "кругов" : "м",
    eventDate: form.elements.eventDate.value,
    trackConfig,
    eventImage,
    calendarImage,
    organizationName,
    organizationColor,
    organizationLogo,
    stages,
    standings: { pilots: [], teams: [] },
  };
  const message = document.querySelector("[data-create-event-message]");
  let apiEvent;
  let createdChampionship = null;
  try {
    if (isChampionship) {
      createdChampionship = await createChampionshipForEvent(createdEvent);
      createdEvent.championshipId = createdChampionship.id;
    }
    apiEvent = await syncEventToApi(createdEvent, "create");
  } catch (error) {
    if (createdChampionship?.id) {
      await fetch(`/api/championships/${encodeURIComponent(createdChampionship.id)}`, { method: "DELETE", credentials: "include" }).catch(() => {});
    }
    if (message) { message.hidden = false; message.textContent = error.message || "Событие не сохранено в базе данных."; }
    showSiteToast(error.message || "Событие не сохранено в базе данных.", "error");
    return;
  }
  const savedEvent = eventFromApi(apiEvent);
  createdEvent.id = savedEvent.id;
  createdEvent.apiId = savedEvent.id;
  const events = getOrganizerEvents();
  events.unshift({ ...createdEvent, ...savedEvent });
  saveOrganizerEvents(events);
  if (message) { message.hidden = false; message.textContent = `${createdEvent.type} создан. Событие добавлено в календарь.`; }
  setTimeout(() => { window.location.href = `/pages/account/CreatedEvent.html?id=${createdEvent.id}`; }, 1000);
});

function formatEventDate(value) {
  if (!value) return "Дата не указана";
  return new Intl.DateTimeFormat("ru-RU", { day: "numeric", month: "long", year: "numeric" }).format(new Date(value));
}

function renderOrganizerCalendarEvents() {
  const stack = document.querySelector(".calendar-series-stack");
  if (!stack) return;
  const events = getOrganizerEvents();
  if (!events.length) return;
  stack.querySelectorAll("[data-created-organization-series]").forEach((block) => block.remove());

  const groups = new Map();
  events.forEach((event) => {
    const key = event.organizationName?.trim() || "Независимый организатор";
    const group = groups.get(key) || [];
    group.push(event);
    groups.set(key, group);
  });

  [...groups.entries()].reverse().forEach(([organizationName, organizationEvents]) => {
    const first = organizationEvents[0];
    const color = first.organizationColor || "#e10600";
    const banner = first.calendarImage || first.eventImage || "/public/drag402banner.jpg";
    const block = document.createElement("section");
    block.className = "calendar-series-block calendar-series-block--created calendar-series-block--organization";
    block.dataset.createdOrganizationSeries = organizationName;
    block.style.setProperty("--series", color);
    block.innerHTML = `<div class="calendar-series-cover calendar-series-cover--created"><img src="${banner}" alt="${escapeHtml(organizationName)}"><div class="calendar-series-cover__shade"></div><div class="calendar-series-cover__content"><a class="button button--white button--small calendar-series-back" href="/championships.html">← ${escapeHtml(first.discipline || "Автоспорт")}</a><div class="calendar-series-cover__title">${first.organizationLogo ? `<span class="calendar-series-cover__logo calendar-series-cover__logo--plain"><img src="${first.organizationLogo}" alt="${escapeHtml(organizationName)}"></span>` : ""}<div><span>Команда организаторов</span><h2>${escapeHtml(organizationName)}</h2><p>${organizationEvents.length} ${organizationEvents.length === 1 ? "событие" : "события"}: чемпионаты, трек-дни и тренировки команды.</p></div></div></div></div><div class="calendar-series-list"></div>`;
    const list = block.querySelector(".calendar-series-list");
    organizationEvents.sort((left, right) => String(left.eventDate).localeCompare(String(right.eventDate))).forEach((item, index) => {
      const article = document.createElement("article");
      article.className = "calendar-series-event calendar-series-event--open";
      article.dataset.stage = String(index + 1).padStart(2, "0");
      article.dataset.calendarType = item.type || "Событие";
      article.dataset.calendarDiscipline = item.discipline || "";
      article.dataset.calendarDate = item.eventDate || "";
      article.innerHTML = `<div class="created-calendar-event__media"><img src="${item.eventImage || item.calendarImage || banner}" alt="${escapeHtml(item.name)}"></div><div class="calendar-series-event__body"><span class="created-calendar-event__type">${escapeHtml(item.type || "Событие")}</span><h3>${escapeHtml(item.name)}</h3><p>${escapeHtml(item.track || "Трасса не указана")} · ${escapeHtml(item.discipline || "Дисциплина не указана")}</p><time>${formatEventDate(item.eventDate)}</time><small>${escapeHtml(item.registrationStatus || "Регистрация закрыта")}</small></div><a class="button button--small calendar-series-event__button" style="background:${color};color:#fff" href="/pages/account/CreatedEvent.html?id=${encodeURIComponent(item.id)}">Подробнее</a>`;
      list.append(article);
    });
    stack.prepend(block);
  });

  if (typeof calendarApiFilterForm !== "undefined" && calendarApiFilterForm) {
    applyCalendarApiFilters(calendarApiFilterForm).catch(() => {});
  }
}

function getEventById(eventId) {
  return getOrganizerEvents().find((item) => item.id === eventId) || null;
}

function getCreatedEventParticipants(eventId) {
  return createdEventParticipantsCache.get(eventId) || [];
}

function saveCreatedEventParticipants(eventId, participants) {
  createdEventParticipantsCache.set(eventId, participants);
}

function getCreatedEventResults(eventId) {
  return createdEventResultsCache.get(eventId) || [];
}

function saveCreatedEventResults(eventId, results) {
  createdEventResultsCache.set(eventId, results);
}

async function loadStartList(eventId) {
  const response = await fetch(`/api/events/${encodeURIComponent(eventId)}/start-list`, { cache: "no-store", credentials: "include" });
  if (!response.ok) throw new Error("Не удалось загрузить стартовый список.");
  const entries = await response.json();
  startListCache.set(eventId, Array.isArray(entries) ? entries : []);
  return entries;
}

function canManageStartList(event) {
  const user = getCurrentUser();
  return user?.role === "Организатор" && (user.id === "organizer-global" || user.id === event.organizerId);
}

function setStartListStatus(message, isError = false) {
  const status = document.querySelector("[data-start-list-status]");
  if (!status) return;
  status.textContent = message;
  status.classList.toggle("is-error", Boolean(isError));
}

function uniquePositiveNumbers(values) {
  return values.every((value) => Number.isInteger(value) && value > 0) && new Set(values).size === values.length;
}

function createdEventTableMode(event) {
  if (isCreatedEventCompleted(event)) return "results";
  return (startListCache.get(event?.id) || []).length ? "start-list" : "registrations";
}

function renderCreatedEventStartList(event) {
  const actions = document.querySelector("[data-start-list-actions]");
  const generateButton = document.querySelector("[data-start-list-generate]");
  const saveButton = document.querySelector("[data-start-list-save]");
  const exportActions = document.querySelector("[data-created-event-results-exports]");
  const canManage = canManageStartList(event);
  const participants = getCreatedEventParticipants(event.id).filter((pilot) => !String(pilot.status || "").toLowerCase().includes("отклон"));
  const entries = startListCache.get(event.id) || [];
  const mode = createdEventTableMode(event);

  if (actions) actions.hidden = !canManage && mode !== "results";
  if (generateButton) {
    generateButton.hidden = mode === "results";
    generateButton.disabled = !canManage || !participants.length;
  }
  if (saveButton) {
    saveButton.hidden = mode !== "start-list";
    saveButton.disabled = !canManage || !entries.length;
  }
  if (exportActions) exportActions.hidden = mode === "registrations";

  if (!participants.length) {
    setStartListStatus("Добавьте участников, затем сформируйте стартовый список.");
  } else if (mode === "registrations") {
    setStartListStatus("Нажмите «Сформировать стартовый список», чтобы назначить позиции и номера.");
  } else if (mode === "start-list") {
    setStartListStatus("Позиции и номера можно изменить, затем сохранить.");
  } else {
    setStartListStatus("Заезд завершён. Доступен экспорт итоговой таблицы.");
  }
  renderCreatedEventParticipants(event.id);
}

async function ensureCreatedEventStartList(event) {
  if (!canManageStartList(event)) return;
  const participants = getCreatedEventParticipants(event.id).filter((pilot) => !String(pilot.status || "").toLowerCase().includes("отклон"));
  if (!participants.length || (startListCache.get(event.id) || []).length) return;
  try {
    const response = await fetch(`/api/events/${encodeURIComponent(event.id)}/start-list/generate`, {
      method: "POST",
      credentials: "include"
    });
    const data = await response.json().catch(() => null);
    if (!response.ok) return;
    startListCache.set(event.id, data.entries || []);
    renderCreatedEventStartList(event);
  } catch (error) {
    // Ручная кнопка формирования остаётся доступной при временной ошибке API.
  }
}

async function generateCreatedEventStartList(event) {
  const button = document.querySelector("[data-start-list-generate]");
  if (button) button.disabled = true;
  try {
    const response = await fetch(`/api/events/${encodeURIComponent(event.id)}/start-list/generate`, { method: "POST", credentials: "include" });
    const data = await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || "Не удалось сформировать стартовый список.");
    startListCache.set(event.id, data.entries || []);
    renderCreatedEventStartList(event);
    showSiteToast((data.entries || []).length ? "Стартовый список сформирован" : "Нет допущенных участников для стартового списка", (data.entries || []).length ? "success" : "error");
  } finally {
    if (button) button.disabled = false;
  }
}

async function saveCreatedEventStartList(event) {
  const rows = [...document.querySelectorAll("[data-created-event-start-list] [data-start-registration-id]")];
  if (!rows.length) throw new Error("Сначала сформируйте стартовый список.");

  const entries = rows.map((row) => ({
    registrationId: Number(row.dataset.startRegistrationId),
    startNumber: Number(row.querySelector("[data-start-number]")?.value),
    startPosition: Number(row.querySelector("[data-start-position]")?.value)
  }));
  if (!uniquePositiveNumbers(entries.map((entry) => entry.startNumber))) throw new Error("Стартовые номера должны быть положительными и не повторяться.");
  if (!uniquePositiveNumbers(entries.map((entry) => entry.startPosition))) throw new Error("Стартовые позиции должны быть положительными и не повторяться.");

  const button = document.querySelector("[data-start-list-save]");
  if (button) button.disabled = true;
  try {
    const response = await fetch(`/api/events/${encodeURIComponent(event.id)}/start-list`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ entries })
    });
    const data = await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || "Не удалось сохранить стартовый список.");
    startListCache.set(event.id, data.entries || []);
    renderCreatedEventStartList(event);
    showSiteToast("Стартовый список сохранен");
  } finally {
    if (button) button.disabled = false;
  }
}

async function completeCreatedEventRace(event) {
  const participants = getCreatedEventParticipants(event.id);
  if (!participants.length) {
    showSiteToast("Нельзя завершить заезд без участников", "error");
    return false;
  }
  const storedResults = getCreatedEventResults(event.id);
  const results = storedResults.length ? storedResults : buildCreatedEventResults(event, participants);
  await syncCreatedEventResultsToApi(event, results);
  const persistedResults = await loadCreatedEventResultsFromApi(event.id);
  if (persistedResults.length !== participants.filter((pilot) => !String(pilot.status || "").toLowerCase().includes("отклон")).length) {
    throw new Error("Не все результаты были сохранены в базе данных.");
  }
  const updatedEvents = getOrganizerEvents().map((item) => item.id === event.id ? { ...item, status: "Завершено", registrationStatus: "Регистрация закрыта" } : item);
  saveOrganizerEvents(updatedEvents);
  await syncEventToApi({ ...event, status: "Завершено", registrationStatus: "Завершено" }, "update");
  renderCreatedEventParticipants(event.id);
  renderCreatedEventResults({ ...event, status: "Завершено", registrationStatus: "Регистрация закрыта" });
  showSiteToast("Заезд завершен, результаты сформированы");
  return true;
}

async function reopenCreatedEventRegistration(event) {
  const updatedEvents = getOrganizerEvents().map((item) => item.id === event.id ? { ...item, status: "Активно", registrationStatus: "Регистрация открыта" } : item);
  saveOrganizerEvents(updatedEvents);
  await syncEventToApi({ ...event, status: "Активно", registrationStatus: "Регистрация открыта" }, "update");
  renderCreatedEventParticipants(event.id);
  renderCreatedEventResults({ ...event, status: "Активно", registrationStatus: "Регистрация открыта" });
  showSiteToast("Регистрация снова открыта");
  return true;
}

function buildCreatedEventResults(event, participants) {
  return participants
    .filter((pilot) => (pilot.status || "Зарегистрирован") !== "Отклонил участие")
    .map((pilot, index) => {
      const seed = parseRaceSeconds(pilot.qualificationTime) ?? (12 + index * 0.37);
      const lap1Ms = secondsToMs(seed + 0.180);
      const lap2Ms = secondsToMs(seed);
      const lap3Ms = secondsToMs(seed + 0.095);
      const bestLapMs = Math.min(lap1Ms, lap2Ms, lap3Ms);
      const penaltyMs = Number(pilot.penaltyMs || 0);
      const finalTimeMs = bestLapMs + penaltyMs;
      return {
        id: `result-${pilot.id || index}`,
        participantId: pilot.id,
        fullName: pilot.fullName,
        driverNumber: pilot.driverNumber || "",
        car: pilot.car,
        className: pilot.className || classifyParticipant(event.discipline, pilot.qualificationTime, pilot.car, resolveClassMode(event)),
        lap1Ms,
        lap2Ms,
        lap3Ms,
        bestLapMs,
        penaltyMs,
        finalTimeMs,
        position: index + 1,
        points: pointsForPosition(index + 1),
        status: pilot.resultStatus || "Финишировал"
      };
    })
    .sort((a, b) => a.finalTimeMs - b.finalTimeMs)
    .map((row, index) => ({ ...row, position: index + 1, points: pointsForPosition(index + 1) }));
}

function renderCreatedEventResults(event) {
  renderCreatedEventParticipants(event.id);
  renderCreatedEventStartList(event);
}

function participantDisplayName(fullName) {
  return String(fullName || "").trim().split(/\s+/).filter(Boolean).slice(0, 2).join(" ") || "—";
}

function classBadge(className) {
  const label = className || "Не указан";
  const key = String(label).toLowerCase().replace(/[^a-zа-я0-9]+/gi, "-").replace(/^-|-$/g, "");
  return `<span class="race-class-badge race-class-badge--${escapeHtml(key)}">${escapeHtml(label)}</span>`;
}

async function rejectCreatedEventParticipant(eventId, participantId) {
  const event = getEventById(eventId);
  const participants = getCreatedEventParticipants(eventId);
  const participant = participants.find((item) => item.id === participantId);
  const currentUser = getCurrentUser();
  if (!event || !participant || currentUser?.role !== "Организатор") return;
  if (!window.confirm(`Отклонить заявку пилота «${participantDisplayName(participant.fullName)}»?`)) return;

  const response = await fetch(`/api/events/${encodeURIComponent(event.apiId || event.id)}/registrations/${encodeURIComponent(participant.apiId || participant.id)}/reject`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      organizerUserId: event.organizerId || currentUser.id,
      email: participant.email
    })
  });
  let data = null;
  try { data = await response.json(); } catch (error) { data = null; }
  if (!response.ok) {
    showSiteToast(data?.message || "Не удалось отклонить заявку пилота", "error");
    return;
  }

  saveCreatedEventParticipants(eventId, participants.filter((item) => item.id !== participant.id));
  saveCreatedEventResults(eventId, getCreatedEventResults(eventId).filter((item) => item.participantId !== participant.id));
  startListCache.delete(eventId);
  renderCreatedEventParticipants(eventId);
  renderCreatedEventStartList(event);
  renderCreatedEventResults(event);
  showSiteToast("Заявка пилота отклонена");
}

function renderCreatedEventParticipants(eventId) {
  const body = document.querySelector("[data-created-event-participants-list]");
  if (!body) return;
  const head = document.querySelector("[data-created-event-table-head]");
  const participants = getCreatedEventParticipants(eventId);
  const results = getCreatedEventResults(eventId);
  const event = getEventById(eventId);
  const entries = startListCache.get(eventId) || [];
  const mode = createdEventTableMode(event);
  const currentUser = getCurrentUser();
  const canReject = mode === "registrations" && currentUser?.role === "Организатор" &&
    (currentUser.id === "organizer-global" || !event?.organizerId || event.organizerId === currentUser.id);
  const registerButton = document.querySelector("[data-created-event-register-open]");
  if (registerButton) registerButton.hidden = mode === "results";

  const titles = {
    registrations: ["Entry list", "Зарегистрированные участники", "Участники, допущенные к событию."],
    "start-list": ["Starting grid", "Стартовый список", "Порядок старта и закреплённые номера пилотов."],
    results: ["Results", "Результаты заезда", "Лучший из трёх кругов, штрафы, позиции и очки."]
  };
  setAccountText("[data-created-event-table-eyebrow]", titles[mode][0]);
  setAccountText("[data-created-event-table-title]", titles[mode][1]);
  setAccountText("[data-created-event-table-description]", titles[mode][2]);

  if (mode === "start-list") {
    if (head) head.innerHTML = "<th>Стартовая позиция</th><th>Стартовый номер</th><th>Пилот</th><th>Команда</th><th>Автомобиль</th><th>Класс</th>";
    if (!entries.length) {
      body.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="6">Стартовый список пока не сформирован.</td></tr>';
      return;
    }
    const canManage = canManageStartList(event);
    body.innerHTML = entries.map((entry, index) => {
      const positionValue = Number(entry.startPosition || 0) || index + 1;
      const numberValue = Number(entry.startNumber || 0) || Number(entry.driverNumber || 0) || index + 1;
      const position = canManage ? `<input class="start-list-input" type="number" min="1" step="1" data-start-position value="${positionValue}" aria-label="Стартовая позиция">` : positionValue;
      const number = canManage ? `<input class="start-list-input" type="number" min="1" step="1" data-start-number value="${numberValue}" aria-label="Стартовый номер">` : numberValue;
      return `<tr data-start-registration-id="${entry.registrationId}"><td>${position}</td><td>${number}</td><td>${escapeHtml(participantDisplayName(entry.driverName))}</td><td>${escapeHtml(entry.teamName || "Нету")}</td><td>${escapeHtml(entry.carName || "—")}</td><td>${classBadge(entry.className || "—")}</td></tr>`;
    }).join("");
    return;
  }

  if (mode === "results") {
    if (head) head.innerHTML = "<th>Позиция</th><th>№ пилота</th><th>Фамилия и имя</th><th>Команда</th><th>Автомобиль</th><th>Класс</th><th>Лучший круг</th><th>Штраф</th><th>Статус</th><th>Очки</th>";
    if (!results.length) {
      body.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="10">Результаты появятся после завершения заезда.</td></tr>';
      return;
    }
    const resultByParticipant = new Map(results.map((row) => [String(row.participantId), row]));
    const rows = participants.map((pilot) => ({
      pilot,
      result: resultByParticipant.get(String(pilot.id)) || results.find((row) => row.fullName === pilot.fullName)
    })).sort((left, right) => (left.result?.position || Number.MAX_SAFE_INTEGER) - (right.result?.position || Number.MAX_SAFE_INTEGER));
    body.innerHTML = rows.map(({ pilot, result }) => {
      const penalty = Number(result?.penaltyMs || 0);
      return `<tr><td>${result?.position || "—"}</td><td>${escapeHtml(pilot.driverNumber || result?.driverNumber || "—")}</td><td>${escapeHtml(participantDisplayName(pilot.fullName))}</td><td>${escapeHtml(pilot.teamName || "Нету")}</td><td>${escapeHtml(pilot.car || result?.car || "—")}</td><td>${classBadge(pilot.className || result?.className || "—")}</td><td>${result?.bestLapMs ? msToLapTime(result.bestLapMs) : "—"}</td><td>${penalty > 0 ? msToLapTime(penalty) : "—"}</td><td>${escapeHtml(result?.status || "—")}</td><td>${result?.points ?? "—"}</td></tr>`;
    }).join("");
    return;
  }

  if (head) head.innerHTML = "<th>#</th><th>№ пилота</th><th>Фамилия и имя</th><th>Команда</th><th>Автомобиль</th><th>Класс</th>" + (canReject ? '<th aria-label="Действия"></th>' : "");
  if (!participants.length) {
    body.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="' + (6 + (canReject ? 1 : 0)) + '">Пока нет зарегистрированных участников.</td></tr>';
    return;
  }
  body.innerHTML = participants.map((pilot, index) => `<tr><td>${index + 1}</td><td>${escapeHtml(pilot.driverNumber || "—")}</td><td>${escapeHtml(participantDisplayName(pilot.fullName))}</td><td>${escapeHtml(pilot.teamName || "Нету")}</td><td>${escapeHtml(pilot.car || "—")}</td><td>${classBadge(pilot.className || "—")}</td>${canReject ? `<td class="drag-entry-table__action"><button class="participant-reject-button" type="button" data-reject-participant="${escapeHtml(pilot.id)}" aria-label="Отклонить заявку">×</button></td>` : ""}</tr>`).join("");
  body.querySelectorAll("[data-reject-participant]").forEach((button) => {
    button.addEventListener("click", () => rejectCreatedEventParticipant(eventId, button.dataset.rejectParticipant));
  });
}

const eventJudgesCache = new Map();

function eventApiKey(event) {
  return event?.apiId || event?.id || "";
}

async function loadEventJudges(event) {
  const key = eventApiKey(event);
  if (!key) return [];
  try {
    const response = await fetch("/api/events/" + encodeURIComponent(key) + "/judges", { credentials: "include", cache: "no-store" });
    if (!response.ok) return [];
    const judges = await response.json();
    eventJudgesCache.set(key, Array.isArray(judges) ? judges : []);
  } catch (error) {
    console.warn("Не удалось загрузить судей события.", error);
    eventJudgesCache.set(key, []);
  }
  return eventJudgesCache.get(key);
}

function judgeLabel(judge) {
  return judge.fullName || judge.login || judge.email || judge.id || "Судья";
}

function canManageCreatedEvent(event) {
  const user = getCurrentUser();
  return user?.role === "Организатор" &&
    (user.id === "organizer-global" || !event?.organizerId || event.organizerId === user.id);
}

async function assignCreatedEventJudge(event, value) {
  const identifier = String(value || "").trim();
  if (!identifier) throw new Error("Укажите почту или номер телефона судьи.");
  const response = await fetch("/api/events/" + encodeURIComponent(eventApiKey(event)) + "/judges", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ identifier })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось отправить приглашение судье.");
  return data;
}

async function removeCreatedEventJudge(event, judgeUserId) {
  const response = await fetch("/api/events/" + encodeURIComponent(eventApiKey(event)) + "/judges/" + encodeURIComponent(judgeUserId), {
    method: "DELETE",
    credentials: "include"
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось удалить судью.");
  eventJudgesCache.set(eventApiKey(event), data.judges || []);
  renderCreatedEventJudges(event);
}

function renderCreatedEventJudges(event) {
  const container = document.querySelector("[data-created-event-judges]");
  if (!container) return;
  const assignedJudges = eventJudgesCache.get(eventApiKey(event)) || [];
  const canManage = canManageCreatedEvent(event);
  const assignedList = assignedJudges.length
    ? `<div class="created-event-judges__line">${assignedJudges.map((judge) => `<span>${escapeHtml(judgeLabel(judge))}${canManage ? ` <button type="button" data-remove-event-judge="${escapeHtml(judge.userId || judge.id)}" aria-label="Удалить судью">×</button>` : ""}</span>`).join("")}</div>`
    : "<p>Судьи пока не назначены.</p>";
  container.innerHTML = `${assignedList}${canManage ? `<form class="created-event-judge-form" data-created-event-judge-form><input type="text" name="judge" autocomplete="off" placeholder="Почта или номер телефона судьи"><button class="button button--red button--small" type="submit">Пригласить судью</button></form>` : ""}`;
  container.querySelector("[data-created-event-judge-form]")?.addEventListener("submit", async (submitEvent) => {
    submitEvent.preventDefault();
    try {
      const data = await assignCreatedEventJudge(event, submitEvent.currentTarget.elements.judge.value);
      submitEvent.currentTarget.reset();
      showSiteToast(data?.message || "Приглашение судье отправлено");
    } catch (error) {
      showSiteToast(error.message || "Не удалось назначить судью", "error");
    }
  });
  container.querySelectorAll("[data-remove-event-judge]").forEach((button) => {
    button.addEventListener("click", async () => {
      try {
        await removeCreatedEventJudge(event, button.dataset.removeEventJudge);
        showSiteToast("Судья удалён из события");
      } catch (error) {
        showSiteToast(error.message || "Не удалось удалить судью", "error");
      }
    });
  });
  return;
  const judges = String(event.judges || "")
    .split(/[;,\n]/)
    .map((name) => name.trim())
    .filter(Boolean)
    .slice(0, 3);
  container.innerHTML = judges.length
    ? `<div class="created-event-judges__line">${judges.map((name) => escapeHtml(name)).join(" <span>·</span> ")}</div>`
    : "<p>Судьи пока не назначены.</p>";
}

function renderCreatedEventStandings(event) {
  const pilotBody = document.querySelector("[data-created-event-pilot-standings]");
  const teamBody = document.querySelector("[data-created-event-team-standings]");
  if (!pilotBody || !teamBody) return;
  const pilots = event.standings?.pilots || [];
  const teams = event.standings?.teams || [];
  pilotBody.innerHTML = pilots.length ? pilots.map((pilot, index) => `<tr><td>${index + 1}</td><td>${pilot.name}</td><td>${pilot.stages || "—"}</td><td>${pilot.total || 0}</td></tr>`).join("") : '<tr><td colspan="4">Пилоты появятся после регистрации на этапы.</td></tr>';
  teamBody.innerHTML = teams.length ? teams.map((team, index) => `<tr><td>${index + 1}</td><td>${team.name}</td><td>${team.stages || "—"}</td><td>${team.total || 0}</td></tr>`).join("") : '<tr><td colspan="4">Команды появятся после регистрации пилотов.</td></tr>';
}

async function initCreatedEventPage() {
  const page = document.querySelector("[data-created-event-page]");
  if (!page) return;
  const eventId = new URLSearchParams(window.location.search).get("id");
  const event = getEventById(eventId);
  if (!event) {
    page.innerHTML = '<section class="section"><div class="container"><div class="account-empty-state account-empty-state--large"><strong>Событие не найдено</strong><p>Вернитесь в календарь и выберите существующее событие.</p><a class="button button--red button--small" href="calendar.html">К календарю</a></div></div></section>';
    return;
  }

  try {
    await loadStartList(event.id);
  } catch (error) {
    startListCache.set(event.id, []);
    setStartListStatus(error.message || "Стартовый список временно недоступен.", true);
  }
  await loadEventJudges(event);
  await loadChampionshipStandings(event);

  const image = document.querySelector("[data-created-event-image]");
  if (image) image.src = event.eventImage || "/public/drag402banner.jpg";
  setAccountText("[data-created-event-type]", `${event.type} · ${event.discipline}`);
  setAccountText("[data-created-event-name]", event.name);
  setAccountText("[data-created-event-subtitle]", `${event.discipline} · ${event.track}`);
  setAccountText("[data-created-event-kind]", event.type);
  setAccountText("[data-created-event-discipline]", event.discipline);
  setAccountText("[data-created-event-distance]", event.distance ? `${event.distance} ${event.distanceLabel}` : "Дистанция не указана");
  setAccountText("[data-created-event-date]", formatEventDate(event.eventDate));
  setAccountText("[data-created-event-track]", event.track);
  setAccountText("[data-created-event-participants]", event.participants);
  setAccountText("[data-created-event-register-title]", `${event.name} · ${formatEventDate(event.eventDate)}`);

  const track = document.querySelector("[data-created-event-track-config]");
  if (track) track.innerHTML = event.trackConfig ? `<img src="${event.trackConfig}" alt="Конфигурация трека ${event.name}">` : "<p>Конфигурация трека не найдена.</p>";

  const stages = document.querySelector("[data-created-event-stages]");
  if (stages) {
    const eventStages = event.type === "Чемпионат" ? (event.stages || []) : [{ name: event.name, date: event.eventDate, intro: `${event.discipline} · ${event.track}`, registrationStatus: "Регистрация открыта" }];
    stages.innerHTML = eventStages.length ? eventStages.map((stage, index) => `<article>${stage.bannerImage ? `<img class="created-event-stage-banner" src="${stage.bannerImage}" alt="Этап ${index + 1}">` : ""}<b>${String(index + 1).padStart(2, "0")}</b><div><h3>${stage.name}</h3><time>${formatEventDate(stage.date)}</time><p>${stage.intro || "Вводная информация не указана."}</p></div><span>${stage.registrationStatus || "Скоро"}</span></article>`).join("") : '<p>Этапы пока не указаны.</p>';
  }

  const standingsSection = document.querySelector("[data-created-event-standings-section]");
  if (standingsSection) standingsSection.hidden = event.type !== "Чемпионат";
  renderCreatedEventStandings(event);
  renderCreatedEventJudges(event);
  renderCreatedEventParticipants(event.id);
  renderCreatedEventStartList(event);
  renderCreatedEventResults(event);
  document.querySelector("[data-start-list-generate]")?.addEventListener("click", async () => {
    try { await generateCreatedEventStartList(event); }
    catch (error) { showSiteToast(error.message, "error"); }
  });
  document.querySelector("[data-start-list-save]")?.addEventListener("click", async () => {
    try { await saveCreatedEventStartList(event); }
    catch (error) { showSiteToast(error.message, "error"); }
  });
  document.querySelectorAll("[data-created-event-results-exports] [data-created-event-export]").forEach((button) => {
    button.addEventListener("click", () => exportCreatedEventResults(event, button.dataset.createdEventExport));
  });

  const tableMenu = document.querySelector("[data-start-list-actions]");
  const tableMenuToggle = document.querySelector("[data-event-table-menu-toggle]");
  const closeTableMenu = () => {
    tableMenu?.classList.remove("is-open");
    tableMenuToggle?.setAttribute("aria-expanded", "false");
  };
  tableMenuToggle?.addEventListener("click", (clickEvent) => {
    clickEvent.stopPropagation();
    const isOpen = tableMenu?.classList.toggle("is-open") || false;
    tableMenuToggle.setAttribute("aria-expanded", String(isOpen));
  });
  document.querySelector("[data-event-table-menu-dropdown]")?.addEventListener("click", (clickEvent) => clickEvent.stopPropagation());
  document.addEventListener("click", closeTableMenu);

  const editButton = document.querySelector("[data-created-event-edit]");
  const currentUser = getCurrentUser();
  if (editButton && canManageCreatedEvent(event)) {
    const managePanel = document.querySelector("[data-created-event-manage]");
    if (managePanel) managePanel.hidden = false;
    editButton.addEventListener("click", () => {
      openSiteModal(`<div class="site-modal__head created-event-edit-head"><span class="eyebrow eyebrow--red">Organizer tools</span><h2>Редактирование события</h2><p>Измените параметры, управляйте регистрацией и завершайте заезд.</p></div><form class="created-event-edit-form" data-created-event-edit-form><label><span>Название события</span><input type="text" name="name" value="${escapeHtml(event.name || "")}" required></label><label><span>Статус события</span><select name="status"><option${event.status === "Активно" ? " selected" : ""}>Активно</option><option${event.status === "Скоро" ? " selected" : ""}>Скоро</option><option${event.status === "Завершено" ? " selected" : ""}>Завершено</option><option${event.status === "Отменено" ? " selected" : ""}>Отменено</option></select></label><label><span>Статус регистрации</span><select name="registrationStatus"><option${eventRegistrationStatusValue(event) === "Регистрация открыта" ? " selected" : ""}>Регистрация открыта</option><option${eventRegistrationStatusValue(event) === "Скоро" ? " selected" : ""}>Скоро</option><option${eventRegistrationStatusValue(event) === "Регистрация закрыта" ? " selected" : ""}>Регистрация закрыта</option><option${eventRegistrationStatusValue(event) === "Недоступно" ? " selected" : ""}>Недоступно</option></select></label><label><span>Назначенные судьи</span><input type="text" name="judges" value="${escapeHtml(event.judges || "")}" placeholder="ФИО или логины судей через запятую"></label><label><span>Трасса</span><input type="text" name="track" value="${escapeHtml(event.track || "")}"></label><label><span>Дата проведения</span><input type="date" name="eventDate" value="${escapeHtml(event.eventDate || "")}"></label><div class="created-event-edit-tools"><span>Управление заездом</span><div><button class="button button--red" type="button" data-created-event-finish-in-modal>Завершить заезд</button><button class="button button--dark" type="button" data-created-event-reopen-in-modal>Включить регистрацию заново</button></div><small>После завершения в таблице участников появятся позиции и очки.</small></div><div class="created-event-edit-form__actions"><button class="button button--red" type="submit">Сохранить изменения</button><button class="button button--dark" type="button" data-created-event-delete>Удалить событие</button></div></form>`, "site-modal--event-edit");
      const eventEditModal = document.querySelector("[data-site-modal]");
      eventEditModal?.classList.remove("site-modal--anchored");
      requestAnimationFrame(() => centerVisibleModal(eventEditModal, { scrollToDialog: false }));
      const editForm = document.querySelector("[data-created-event-edit-form]");
      editForm?.addEventListener("submit", async (submitEvent) => {
        submitEvent.preventDefault();
        const form = submitEvent.currentTarget;
        const patch = { name: form.elements.name.value.trim(), status: form.elements.status.value, registrationStatus: form.elements.registrationStatus.value, judges: form.elements.judges.value.trim(), track: form.elements.track.value.trim(), eventDate: form.elements.eventDate.value };
        const updatedEvent = { ...event, ...patch };
        const apiEvent = await syncEventToApi(updatedEvent, "update");
        const savedEvent = eventFromApi(apiEvent);
        saveOrganizerEvents(getOrganizerEvents().map((item) => item.id === event.id ? savedEvent : item));
        showSiteToast("Событие обновлено");
        setTimeout(() => window.location.reload(), 500);
      });
      document.querySelector("[data-created-event-finish-in-modal]")?.addEventListener("click", async () => {
        try {
          if (await completeCreatedEventRace(event)) closeSiteModal();
        } catch (error) {
          showSiteToast(error.message || "Не удалось завершить заезд", "error");
        }
      });
      document.querySelector("[data-created-event-reopen-in-modal]")?.addEventListener("click", async () => {
        try {
          await reopenCreatedEventRegistration(event);
          closeSiteModal();
          setTimeout(() => window.location.reload(), 350);
        } catch (error) {
          showSiteToast(error.message || "Не удалось открыть регистрацию", "error");
        }
      });
      document.querySelector("[data-created-event-delete]")?.addEventListener("click", async () => {
        if (!window.confirm(`Удалить событие «${event.name}» из календаря?`)) return;
        const response = await fetch(`/api/events/${encodeURIComponent(event.apiId || event.id)}`, { method: "DELETE" });
        if (!response.ok) { showSiteToast("Не удалось удалить событие из БД", "error"); return; }
        saveOrganizerEvents(getOrganizerEvents().filter((item) => item.id !== event.id));
        createdEventParticipantsCache.delete(event.id);
        createdEventResultsCache.delete(event.id);
        showSiteToast("Событие удалено");
        setTimeout(() => { window.location.href = "/calendar.html"; }, 500);
      });
    });
  }

  const modal = document.querySelector(".drag-register-modal");
  const form = document.querySelector("[data-created-event-register-form]");

  function openCreatedEventRegisterModal() {
    if (!modal) return;
    if (!isEventRegistrationOpen(event)) {
      showSiteToast("Регистрация на событие закрыта", "error");
      return;
    }
    populateDragRegisterCars();
    const user = getCurrentUser();
    if (form) {
      const timeField = form.querySelector("[data-registration-time-field]");
      const needsTime = eventNeedsQualificationTime(event);
      if (timeField) timeField.hidden = !needsTime;
      if (form.elements.qualificationTime) form.elements.qualificationTime.required = needsTime;
    }
    if (user && form) {
      const fullName = userFullName(user);
      if (fullName) form.elements.fullName.value = fullName;
      if (user.email) form.elements.email.value = user.email;
      if (user.profile?.phone) form.elements.phone.value = user.profile.phone;
    }
    modal.classList.add("is-open");
    modal.setAttribute("aria-hidden", "false");
    document.body.classList.add("modal-open");
    const dialog = modal.querySelector(".drag-register-modal__dialog");
    if (dialog) dialog.scrollTop = 0;
    requestAnimationFrame(() => centerVisibleModal(modal));
    form?.elements.fullName?.focus();
  }

  function closeCreatedEventRegisterModal() {
    if (!modal) return;
    modal.classList.remove("is-open");
    modal.setAttribute("aria-hidden", "true");
    document.body.classList.remove("modal-open");
  }

  document.querySelector("[data-created-event-register-open]")?.addEventListener("click", openCreatedEventRegisterModal);
  document.querySelectorAll("[data-created-event-register-close]").forEach((button) => button.addEventListener("click", closeCreatedEventRegisterModal));
  document.addEventListener("keydown", (keyEvent) => {
    if (keyEvent.key === "Escape" && modal?.classList.contains("is-open")) closeCreatedEventRegisterModal();
  });


  form?.addEventListener("submit", async (submitEvent) => {
    submitEvent.preventDefault();
    const form = submitEvent.currentTarget;
    const selectedVehicle = getSelectedRegistrationVehicle(form);
    const manualCar = form.elements.car.value.trim();
    const carName = selectedVehicle?.name || manualCar;
    if (!carName) { showSiteToast("Выберите автомобиль из профиля или введите его вручную", "error"); return; }
    const needsTime = eventNeedsQualificationTime(event);
    const qualificationTime = parseRaceSeconds(form.elements.qualificationTime?.value);
    if (needsTime && qualificationTime === null) { showSiteToast("Укажите тестовое время для определения класса", "error"); return; }
    const classMode = disciplineKey(event.discipline) === "drag" ? resolveClassMode(event) : "StandardTimeAttack";
    const className = needsTime ? classifyParticipant(event.discipline, qualificationTime, carName, classMode) : "Не требуется";
    const currentUser = getCurrentUser();
    const participant = { id: `participant-${Date.now()}`, fullName: form.elements.fullName.value.trim(), email: form.elements.email.value.trim().toLowerCase(), phone: form.elements.phone.value.trim(), driverNumber: currentUser?.profile?.driverNumber || "", teamName: userRacingTeamName(currentUser), car: carName, qualificationTime, className, classMode, carPower: selectedVehicle?.power || "", carId: selectedVehicle?.id || "" };
    try {
      const data = await syncParticipantToApi(event, participant);
      saveCreatedEventParticipants(event.id, (data.event?.participants || []).map(participantFromApi));
      createdEventResultsCache.delete(event.id);
      startListCache.delete(event.id);
    } catch (error) {
      showSiteToast(error.message || "Заявка не сохранена", "error");
      return;
    }
    renderCreatedEventParticipants(event.id);
    renderCreatedEventStartList(event);
    renderCreatedEventResults(event);
    form.reset();
    closeCreatedEventRegisterModal();
    showSiteToast("Заявка на событие отправлена");
  });
}

async function initializeDatabaseBackedCompetitionPages() {
  try {
    await serverSessionPromise;
    await loadDatabaseEventState();
    renderDragParticipants();
    renderOrganizerCalendarEvents();
    await initCreatedEventPage();
    if (getCurrentUser()?.role === "Судья" && document.querySelector("[data-judge-event-select]")) await loadAssignedJudgeEvents();
    renderJudgeEvents();
  } catch (error) {
    console.error(error);
    if (document.querySelector("[data-created-event-page]")) showSiteToast(error.message, "error");
  }
}

void initializeDatabaseBackedCompetitionPages();

let judgeResultsCache = {};

function getJudgeResults() {
  return judgeResultsCache;
}

function saveJudgeResults(results) {
  judgeResultsCache = results;
}

function calculateRacePoints(position, status = "") {
  if (["Дисквалификация", "Дисквалифицирован"].includes(status)) return 0;
  const pointsByPosition = { 1: 25, 2: 18, 3: 15, 4: 12, 5: 10, 6: 8, 7: 6, 8: 4, 9: 2, 10: 1 };
  return pointsByPosition[Number(position)] || 0;
}

let assignedJudgeEventsCache = null;

async function loadAssignedJudgeEvents() {
  try {
    const response = await fetch("/api/events/judge-assigned", { credentials: "include", cache: "no-store" });
    if (response.status === 401 || response.status === 403) {
      assignedJudgeEventsCache = [];
      return;
    }
    if (!response.ok) throw new Error("Не удалось загрузить назначенные события.");
    const events = await response.json();
    assignedJudgeEventsCache = Array.isArray(events)
      ? events.map((event) => ({
          id: event.id,
          name: event.name || event.title || "Событие",
          type: event.type || "",
          date: event.date || event.eventDate || "",
          track: event.track || "",
          discipline: event.discipline || "",
          participants: Array.isArray(event.participants) ? event.participants : [],
          created: true
        }))
      : [];
  } catch (error) {
    assignedJudgeEventsCache = [];
    showSiteToast(error.message || "Не удалось загрузить события судьи", "error");
  }
}

function getJudgeEvents() {
  return Array.isArray(assignedJudgeEventsCache) ? assignedJudgeEventsCache : [];
}

function getJudgeParticipants(eventId) {
  if (!eventId) return [];
  const assigned = getJudgeEvents().find((event) => String(event.id) === String(eventId));
  if (assigned) return assigned.participants || [];
  const created = getEventById(eventId);
  if (created) return getCreatedEventParticipants(eventId);
  return [];
}

function renderJudgeEvents() {
  const select = document.querySelector("[data-judge-event-select]");
  const table = document.querySelector("[data-judge-events-table]");
  if (!select && !table) return;
  const events = getJudgeEvents();
  if (select) {
    const previous = select.value;
    select.innerHTML = events.length
      ? events.map((event) => `<option value="${escapeHtml(event.id)}">${escapeHtml(event.name)} · ${formatEventDate(event.date)}</option>`).join("")
      : '<option value="">Нет назначенных соревнований</option>';
    if (previous && events.some((event) => String(event.id) === previous)) select.value = previous;
  }
  if (table) {
    table.innerHTML = events.length
      ? events.map((event) => `<tr data-judge-assigned-event="${escapeHtml(event.id)}"><td>${escapeHtml(event.name)}</td><td>${formatEventDate(event.date)}</td><td>${escapeHtml(event.track || "—")}</td><td>${escapeHtml(event.discipline || "—")}</td><td>${(event.participants || []).length}</td><td><button class="judge-row-action" type="button" data-judge-select-event="${escapeHtml(event.id)}">Выбрать</button></td></tr>`).join("")
      : '<tr><td colspan="6">У вас пока нет назначенных соревнований.</td></tr>';
  }
  renderJudgeParticipants();
  renderJudgeResults();
}

function renderJudgeParticipants() {
  const eventSelect = document.querySelector("[data-judge-event-select]");
  const participantSelect = document.querySelector("[data-judge-participant-select]");
  const table = document.querySelector("[data-judge-participants-table]");
  if (!eventSelect) return;
  const participants = getJudgeParticipants(eventSelect.value);
  if (participantSelect) {
    participantSelect.innerHTML = '<option value="">Ввести вручную</option>' + participants.map((pilot) => `<option value="${escapeHtml(pilot.id || pilot.email || pilot.fullName)}">${escapeHtml(pilot.fullName)} · ${escapeHtml(pilot.car || "автомобиль не указан")}</option>`).join("");
  }
  if (table) {
    table.innerHTML = participants.length
      ? participants.map((pilot) => `<tr><td>${escapeHtml(participantDisplayName(pilot.fullName))}</td><td>${escapeHtml(pilot.teamName || "Нету")}</td><td>${escapeHtml(pilot.car || "—")}</td><td>${classBadge(pilot.className || "—")}</td><td><button class="judge-row-action" type="button" data-judge-fill-participant="${escapeHtml(pilot.id || pilot.email || pilot.fullName)}">Внести</button></td></tr>`).join("")
      : '<tr><td colspan="5">В выбранном событии пока нет заявок.</td></tr>';
  }
}

function getSelectedJudgeResults() {
  const eventId = document.querySelector("[data-judge-event-select]")?.value;
  const results = getJudgeResults();
  return { eventId, results, rows: eventId ? (results[eventId] || []) : [] };
}

function renderJudgeResults() {
  const body = document.querySelector("[data-judge-results]");
  if (!body) return;
  const { rows } = getSelectedJudgeResults();
  const exportPanel = document.querySelector("[data-judge-results-exports]");
  if (exportPanel) exportPanel.hidden = !rows.length;
  if (!rows.length) {
    body.innerHTML = '<tr><td colspan="8">Результаты пока не внесены.</td></tr>';
    return;
  }
  body.innerHTML = rows
    .slice()
    .sort((a, b) => (Number(a.position) || 999) - (Number(b.position) || 999))
    .map((row) => {
      const isDsq = row.status === "Дисквалификация" || row.status === "Дисквалифицирован";
      return `<tr data-judge-result-id="${escapeHtml(row.id)}"><td>${row.position || "—"}</td><td>${escapeHtml(row.pilotName || row.driverName || "—")}</td><td>${escapeHtml(row.finalTimeMs ? msToLapTime(row.finalTimeMs) : row.lapTime || "—")}</td><td>${escapeHtml(row.bestLapMs ? msToLapTime(row.bestLapMs) : row.bestLap || "—")}</td><td><strong>${Number.isFinite(Number(row.points)) ? row.points : calculateRacePoints(row.position, row.status)}</strong></td><td>${escapeHtml(row.penaltyMs ? msToLapTime(row.penaltyMs) : row.penalty || "—")}</td><td><span class="judge-status ${isDsq ? "judge-status--dsq" : ""}">${escapeHtml(row.status || "Финишировал")}</span></td><td><div class="judge-result-actions"><button type="button" data-judge-add-penalty="${escapeHtml(row.id)}">Штраф</button><button type="button" data-judge-disqualify="${escapeHtml(row.id)}">DSQ</button></div></td></tr>`;
    })
    .join("");
}

async function exportSelectedJudgeResults(format) {
  const { eventId, rows } = getSelectedJudgeResults();
  if (!eventId) {
    showSiteToast("Выберите событие для экспорта", "error");
    return;
  }
  if (!rows.length) {
    showSiteToast("В выбранном событии пока нет результатов", "error");
    return;
  }
  const event = getJudgeEvents().find((item) => String(item.id) === String(eventId));
  await downloadCreatedEventReportFromApi({
    id: eventId,
    name: event?.name || "RaceManager-results"
  }, format);
}

document.querySelectorAll("[data-judge-results-exports] [data-judge-export]").forEach((button) => {
  button.addEventListener("click", async () => {
    const label = button.textContent;
    try {
      button.disabled = true;
      button.textContent = "Готовим...";
      await exportSelectedJudgeResults(button.dataset.judgeExport);
    } catch (error) {
      showSiteToast(error.message || "Не удалось экспортировать результаты", "error");
    } finally {
      button.disabled = false;
      button.textContent = label;
    }
  });
});

function fillJudgeForm(result) {
  const form = document.querySelector("[data-judge-result-form]");
  if (!form || !result) return;
  form.elements.pilotName.value = result.pilotName || "";
  form.elements.position.value = result.position || "";
  if (form.elements.lap1) form.elements.lap1.value = result.lap1Ms ? msToLapTime(result.lap1Ms) : "";
  if (form.elements.lap2) form.elements.lap2.value = result.lap2Ms ? msToLapTime(result.lap2Ms) : "";
  if (form.elements.lap3) form.elements.lap3.value = result.lap3Ms ? msToLapTime(result.lap3Ms) : "";
  form.elements.lapTime.value = result.finalTimeMs ? msToLapTime(result.finalTimeMs) : (result.lapTime || "");
  form.elements.bestLap.value = result.bestLapMs ? msToLapTime(result.bestLapMs) : (result.bestLap || "");
  form.elements.penalty.value = result.penaltyMs ? msToLapTime(result.penaltyMs) : (result.penalty || "");
  form.elements.status.value = result.status || "Финишировал";
  form.elements.comment.value = result.comment || "";
  form.dataset.editingResultId = result.id;
}

document.querySelector("[data-judge-event-select]")?.addEventListener("change", () => {
  renderJudgeParticipants();
  renderJudgeResults();
});

document.querySelector("[data-judge-events-table]")?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-judge-select-event]");
  if (!button) return;
  const select = document.querySelector("[data-judge-event-select]");
  if (select) {
    select.value = button.dataset.judgeSelectEvent;
    renderJudgeParticipants();
    renderJudgeResults();
  }
});

document.querySelector("[data-judge-participants-table]")?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-judge-fill-participant]");
  if (!button) return;
  const select = document.querySelector("[data-judge-participant-select]");
  if (select) {
    select.value = button.dataset.judgeFillParticipant;
    select.dispatchEvent(new Event("change", { bubbles: true }));
  }
});

document.querySelector("[data-judge-participant-select]")?.addEventListener("change", (event) => {
  const participant = getJudgeParticipants(document.querySelector("[data-judge-event-select]")?.value).find((item) => String(item.id || item.email || item.fullName) === event.currentTarget.value);
  const form = document.querySelector("[data-judge-result-form]");
  if (participant && form) {
    form.elements.pilotName.value = participant.fullName;
    form.elements.bestLap.value = participant.qualificationTime ? msToLapTime(secondsToMs(participant.qualificationTime)) : form.elements.bestLap.value;
  }
});

document.querySelector("[data-judge-result-form]")?.addEventListener("input", (event) => {
  const form = event.currentTarget;
  if (["lap1", "lap2", "lap3", "penalty"].includes(event.target.name)) calculateJudgeFormTimes(form);
});

document.querySelector("[data-judge-result-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const eventId = form.elements.eventId.value;
  const store = getJudgeResults();
  const rows = store[eventId] || [];
  const id = form.dataset.editingResultId || `result-${Date.now()}`;
  const calculated = calculateJudgeFormTimes(form);
  const result = {
    id,
    pilotName: form.elements.pilotName.value.trim(),
    position: form.elements.position.value,
    lapTime: form.elements.lapTime.value.trim(),
    bestLap: form.elements.bestLap.value.trim(),
    lap1Ms: raceInputToMs(form.elements.lap1.value),
    lap2Ms: raceInputToMs(form.elements.lap2.value),
    lap3Ms: raceInputToMs(form.elements.lap3.value),
    penalty: form.elements.penalty.value.trim(),
    penaltyMs: calculated.penaltyMs,
    finalTimeMs: calculated.finalTimeMs,
    status: form.elements.status.value,
    points: calculateRacePoints(form.elements.position.value, form.elements.status.value),
    comment: form.elements.comment.value.trim(),
    updatedAt: new Date().toISOString(),
  };
  const participantId = document.querySelector("[data-judge-participant-select]")?.value || null;
  const response = await fetch("/api/results", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ eventId, participantId, driverName: result.pilotName, position: Number(result.position), lapTime: result.lapTime, bestLap: result.bestLap, points: result.points, status: result.status, judgeUserId: getCurrentUser()?.id || null, lap1Ms: result.lap1Ms, lap2Ms: result.lap2Ms, lap3Ms: result.lap3Ms, penaltyMs: result.penaltyMs, finalTimeMs: result.finalTimeMs })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) { showSiteToast(data?.message || "Не удалось сохранить результат в БД", "error"); return; }
  result.id = data.result.id;
  const index = rows.findIndex((item) => item.id === id || item.pilotName.toLowerCase() === result.pilotName.toLowerCase());
  if (index === -1) rows.push(result);
  else rows[index] = { ...rows[index], ...result };
  store[eventId] = rows;
  saveJudgeResults(store);
  await reloadJudgeEventResults(eventId);
  form.removeAttribute("data-editing-result-id");
  form.reset();
  renderJudgeParticipants();
  renderJudgeResults();
  const message = document.querySelector("[data-judge-save-message]");
  if (message) { message.hidden = false; message.textContent = "Результат сохранен"; setTimeout(() => { message.hidden = true; }, 2500); }
});

async function applyJudgePenalty(resultId) {
  const reason = window.prompt("Причина штрафа:", "Нарушение регламента");
  if (reason === null) return;
  const pointsText = window.prompt("Сколько очков снять?", "0");
  if (pointsText === null) return;
  const timeText = window.prompt("Сколько секунд добавить ко времени?", "0");
  if (timeText === null) return;
  const timeMs = Math.max(0, Math.round((Number(String(timeText).replace(",", ".")) || 0) * 1000));
  const response = await fetch(`/api/results/${encodeURIComponent(resultId)}/penalties`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason: reason.trim() || "Штраф", points: Math.max(0, Number(pointsText) || 0), timeMs })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось добавить штраф.");
  await reloadJudgeEventResults(data.result.eventId);
  showSiteToast("Штраф добавлен, позиции и очки пересчитаны");
}

async function reloadJudgeEventResults(eventId) {
  const response = await fetch(`/api/results?eventId=${encodeURIComponent(eventId)}&sort=time`, { credentials: "include", cache: "no-store" });
  if (!response.ok) throw new Error("Не удалось обновить таблицу результатов.");
  const store = getJudgeResults();
  store[eventId] = (await response.json()).map((apiResult) => {
    const result = resultFromApi(apiResult);
    return {
      id: result.id, pilotName: result.fullName, position: result.position,
      lapTime: msToLapTime(result.finalTimeMs), bestLap: msToLapTime(result.bestLapMs),
      penalty: result.penalty || (result.penaltyMs ? msToLapTime(result.penaltyMs) : ""),
      lap1Ms: result.lap1Ms, lap2Ms: result.lap2Ms, lap3Ms: result.lap3Ms,
      bestLapMs: result.bestLapMs, finalTimeMs: result.finalTimeMs,
      penaltyMs: result.penaltyMs, status: result.status, points: result.points,
      updatedAt: new Date().toISOString()
    };
  });
  saveJudgeResults(store);
  renderJudgeResults();
}

async function applyJudgeDisqualification(resultId) {
  const reason = window.prompt("Причина дисквалификации:", "Дисквалификация");
  if (reason === null) return;
  const response = await fetch(`/api/results/${encodeURIComponent(resultId)}/disqualify`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason: reason.trim() || "Дисквалификация" })
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || "Не удалось дисквалифицировать участника.");
  await reloadJudgeEventResults(data.result.eventId);
  showSiteToast("Участник дисквалифицирован, таблица пересчитана");
}

function updateJudgeResultFromApi(apiResult) {
  if (!apiResult?.eventId) return;
  const store = getJudgeResults();
  const rows = store[apiResult.eventId] || [];
  const result = resultFromApi(apiResult);
  const judgeRow = {
    id: result.id,
    pilotName: result.fullName,
    position: result.position,
    lapTime: msToLapTime(result.finalTimeMs),
    bestLap: msToLapTime(result.bestLapMs),
    penalty: result.penalty || (result.penaltyMs ? msToLapTime(result.penaltyMs) : ""),
    lap1Ms: result.lap1Ms,
    lap2Ms: result.lap2Ms,
    lap3Ms: result.lap3Ms,
    bestLapMs: result.bestLapMs,
    finalTimeMs: result.finalTimeMs,
    penaltyMs: result.penaltyMs,
    status: result.status,
    points: result.points,
    updatedAt: new Date().toISOString()
  };
  const index = rows.findIndex((row) => row.id === judgeRow.id);
  if (index === -1) rows.push(judgeRow);
  else rows[index] = { ...rows[index], ...judgeRow };
  store[apiResult.eventId] = rows;
  saveJudgeResults(store);
  renderJudgeResults();
}

document.querySelector("[data-judge-results]")?.addEventListener("click", async (event) => {
  const penaltyButton = event.target.closest("[data-judge-add-penalty]");
  const dsqButton = event.target.closest("[data-judge-disqualify]");
  try {
    if (penaltyButton) { await applyJudgePenalty(penaltyButton.dataset.judgeAddPenalty); return; }
    if (dsqButton) { await applyJudgeDisqualification(dsqButton.dataset.judgeDisqualify); return; }
  } catch (error) {
    showSiteToast(error.message || "Не удалось применить решение судьи", "error");
    return;
  }
  const row = event.target.closest("[data-judge-result-id]");
  if (!row) return;
  const { rows } = getSelectedJudgeResults();
  fillJudgeForm(rows.find((item) => item.id === row.dataset.judgeResultId));
});


document.querySelector("[data-judge-import-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const preview = document.querySelector("[data-judge-import-preview]");
  const eventId = document.querySelector("[data-judge-event-select]")?.value || "";
  const file = form.elements.file.files?.[0];
  if (!file) { showSiteToast("Выберите файл для импорта", "error"); return; }
  const data = new FormData();
  data.append("file", file);
  data.append("eventId", eventId);
  data.append("saveResults", form.elements.saveResults.checked ? "true" : "false");
  if (form.elements.saveResults.checked && !eventId) { showSiteToast("Выберите событие для сохранения результатов", "error"); return; }
  const response = await fetch("/api/reports/import", { method: "POST", credentials: "include", body: data });
  const result = await response.json().catch(() => null);
  if (!response.ok) { showSiteToast(result?.message || "Импорт не выполнен", "error"); return; }
  if (preview) {
    preview.hidden = false;
    preview.textContent = `Файл: ${result.file}
Символов: ${result.characters}
Распознано строк: ${(result.parsedRows || []).length}
Сохранено: ${result.imported || 0}

${result.preview || ""}`;
  }
  if (result.imported) {
    await loadDatabaseEventState();
    renderJudgeResults();
  }
  showSiteToast(result.imported ? `Импортировано результатов: ${result.imported}` : "Файл прочитан, показан предпросмотр");
});

async function supportApi(path, options = {}) {
  const response = await fetch(path, {
    credentials: "include",
    ...options,
    headers: { "Content-Type": "application/json", ...(options.headers || {}) }
  });
  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    throw new Error(data.message || "Ошибка API поддержки");
  }
  return response.json();
}

function buildSupportEmailHtml(ticket, message) {
  const esc = (value) => String(value || "").replace(/[&<>"]/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;" }[char]));
  return '<!doctype html><html lang="ru"><body style="margin:0;background:#f4f4f6;font-family:Arial,sans-serif;color:#15161b"><div style="max-width:760px;margin:0 auto;padding:36px 18px"><div style="overflow:hidden;border-radius:0 0 18px 18px;background:#111216;border-bottom:4px solid #e10600"><div style="padding:54px 28px;text-align:center;background:radial-gradient(circle at center,#7b0603 0,#17181d 44%,#101116 100%)"><strong style="font-size:34px;letter-spacing:.08em;color:#fff">RACEMANAGER <span style="display:inline-block;margin-left:10px;padding:8px 14px;border-radius:10px;background:#e10600">ID</span></strong></div></div><div style="padding:48px 36px;background:#fff"><h1 style="margin:0 0 18px;font-size:34px;line-height:1.15">Ответ технической поддержки</h1><p style="margin:0 0 22px;font-size:16px;line-height:1.6;color:#656974">Здравствуйте, <b>' + esc(ticket.name) + '</b>. Мы рассмотрели ваше обращение: <b>' + esc(ticket.subject) + '</b>.</p><div style="padding:22px;border-left:4px solid #e10600;background:#f6f7f9;font-size:16px;line-height:1.7">' + esc(message).replaceAll("\n", "<br>") + '</div><p style="margin:28px 0 0;color:#858994;font-size:13px">С уважением, команда RaceManager.</p></div></div></body></html>';
}

async function createSupportTicket(payload) {
  return supportApi("/api/support/tickets", { method: "POST", body: JSON.stringify(payload) });
}

async function getSupportTickets() {
  return supportApi("/api/support/tickets");
}

async function answerSupportTicket(ticketId, message) {
  const adminUserId = getCurrentUser()?.id || "";
  const response = await supportApi("/api/support/tickets/" + encodeURIComponent(ticketId) + "/answer", { method: "POST", body: JSON.stringify({ adminUserId, message }) });
  return response.ticket;
}

async function rejectSupportTicket(ticketId, reason = "Обращение отклонено техническим администратором.") {
  const adminUserId = getCurrentUser()?.id || "";
  const response = await supportApi("/api/support/tickets/" + encodeURIComponent(ticketId) + "/reject", { method: "POST", body: JSON.stringify({ adminUserId, reason }) });
  return response.ticket;
}

function supportStatusClass(status) {
  if (status === "Рассмотренное") return "is-reviewed";
  if (status === "Отклонено") return "is-rejected";
  return "is-waiting";
}

function formatSupportDate(value) {
  if (!value) return "—";
  return new Date(value).toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function openSupportTicketModal(ticket) {
  openSiteModal('<section class="support-ticket-modal"><span class="eyebrow eyebrow--red">RaceManager support</span><h2>' + ticket.subject + '</h2><div class="support-ticket-modal__meta"><span>' + ticket.name + '</span><a href="mailto:' + ticket.email + '">' + ticket.email + '</a><b class="support-ticket-status ' + supportStatusClass(ticket.status) + '">' + ticket.status + '</b></div><p class="support-ticket-modal__message">' + ticket.message + '</p><form data-support-ticket-answer-form data-ticket-id="' + ticket.id + '"><label><span>Ответ пользователю</span><textarea name="answer" required></textarea></label><div class="support-ticket-modal__actions"><button class="button button--red" type="submit">Отправить ответ</button><button class="button button--dark" type="button" data-support-reject="' + ticket.id + '">Отклонить</button></div></form></section>', 'site-modal--support-ticket');
}

async function renderSupportTickets() {
  const container = document.querySelector("[data-support-tickets]");
  if (!container) return;
  const tickets = await getSupportTickets();
  const waiting = tickets.filter((ticket) => ticket.status === "Ожидание").length;
  const reviewed = tickets.filter((ticket) => ticket.status === "Рассмотренное").length;
  const rejected = tickets.filter((ticket) => ticket.status === "Отклонено").length;
  setAccountText('[data-support-stat="waiting"]', waiting);
  setAccountText('[data-support-stat="reviewed"]', reviewed);
  setAccountText('[data-support-stat="rejected"]', rejected);
  container.replaceChildren();
  if (!tickets.length) {
    container.innerHTML = '<div class="support-ticket-empty"><strong>Обращений пока нет</strong><p>Новые сообщения пользователей появятся в этом списке.</p></div>';
    return;
  }
  tickets.forEach((ticket) => {
    const card = document.createElement("button");
    card.type = "button";
    card.className = "support-ticket-card " + supportStatusClass(ticket.status);
    card.innerHTML = '<span class="support-ticket-status ' + supportStatusClass(ticket.status) + '">' + ticket.status + '</span><div><small>' + ticket.category + ' · ' + formatSupportDate(ticket.createdAtUtc) + '</small><strong>' + ticket.subject + '</strong><p>' + ticket.message + '</p></div><dl><dt>Отправитель</dt><dd>' + ticket.name + '</dd><dt>Email</dt><dd>' + ticket.email + '</dd></dl>';
    card.addEventListener("click", () => openSupportTicketModal(ticket));
    container.append(card);
  });
}

function latestSupportDeliveryStatus(ticket) {
  const answers = ticket?.answers || [];
  return answers[answers.length - 1]?.emailDeliveryStatus || "Не отправлено";
}

document.addEventListener("submit", async (event) => {
  if (!event.target.matches("[data-support-ticket-answer-form]")) return;
  event.preventDefault();
  const form = event.target;
  try {
    const ticket = await answerSupportTicket(form.dataset.ticketId, form.elements.answer.value.trim());
    closeSiteModal();
    await renderSupportTickets();
    const delivered = latestSupportDeliveryStatus(ticket) === "Отправлено";
    showSiteToast(delivered ? "Ответ сохранён и отправлен на почту" : "Ответ успешно сохранён");
  } catch (error) {
    showSiteToast(error.message || "Не удалось сохранить ответ", "error");
  }
});

document.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-support-reject]");
  if (!button) return;
  try {
    const ticket = await rejectSupportTicket(button.dataset.supportReject);
    closeSiteModal();
    await renderSupportTickets();
    const delivered = latestSupportDeliveryStatus(ticket) === "Отправлено";
    showSiteToast(delivered ? "Обращение отклонено, уведомление отправлено" : "Обращение успешно отклонено");
  } catch (error) {
    showSiteToast(error.message || "Не удалось отклонить обращение", "error");
  }
});

if (accountPage && ["Технический админ", "Технический администратор"].includes(getCurrentUser()?.role)) {
  renderSupportTickets();
}


async function loadCreatedTeamCatalogs() {
  const organizerCatalog = document.querySelector("[data-organizer-team-catalog]");
  const racingCatalog = document.querySelector("[data-racing-team-catalog]");
  if (!organizerCatalog || !racingCatalog) return;
  try {
    const response = await fetch("/api/users/team-catalog");
    if (!response.ok) return;
    const users = await response.json();
    const organizerNames = new Set([...organizerCatalog.querySelectorAll("h2")].map((item) => item.textContent.trim().toLowerCase()));
    const racingNames = new Set([...racingCatalog.querySelectorAll("h2")].map((item) => item.textContent.trim().toLowerCase()));
    users.forEach((user) => {
      const profile = user.profile || {};
      if (profile.organizationName && !organizerNames.has(profile.organizationName.toLowerCase())) {
        organizerCatalog.append(createCatalogTeamCard({
          name: profile.organizationName,
          type: "Команда организаторов",
          color: profile.organizationColor,
          logo: profile.organizationLogo,
          banner: profile.organizationBanner,
          members: profile.organizationMembers
        }));
        organizerNames.add(profile.organizationName.toLowerCase());
      }
      if (profile.racingTeamName && !racingNames.has(profile.racingTeamName.toLowerCase())) {
        racingCatalog.append(createCatalogTeamCard({
          name: profile.racingTeamName,
          type: "Гоночная команда",
          color: profile.racingTeamColor,
          logo: profile.racingTeamLogo,
          banner: profile.racingTeamBanner,
          members: profile.racingTeamMembers
        }));
        racingNames.add(profile.racingTeamName.toLowerCase());
      }
    });
  } catch (error) {
    console.warn("Не удалось загрузить созданные команды.", error);
  }
}

function createCatalogTeamCard(team) {
  const card = document.createElement("article");
  card.className = "teams-catalog-card teams-catalog-card--created";
  card.style.setProperty("--team-color", team.color || "#e10600");
  if (team.banner) card.style.setProperty("--team-banner", 'url("' + team.banner + '")');
  const members = (team.members || []).slice(0, 4).map((member) => '<span>' + escapeHtml(member.fullName) + '</span>').join("");
  const logo = team.logo ? '<span class="teams-catalog-card__logo"><img src="' + team.logo + '" alt=""></span>' : '<span class="teams-catalog-card__logo teams-catalog-card__logo--text">RM</span>';
  card.innerHTML = '<span class="teams-catalog-card__type">' + escapeHtml(team.type) + '</span><h2>' + escapeHtml(team.name) + '</h2>' + logo + '<div class="teams-catalog-card__drivers">' + (members || "<span>Состав пока не добавлен</span>") + "</div>";
  return card;
}

loadCreatedTeamCatalogs();

if (accountPage) setInterval(() => refreshAccountTeamState(false), 10000);
