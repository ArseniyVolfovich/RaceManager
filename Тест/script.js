const menuButton = document.querySelector(".menu-toggle");
const menu = document.querySelector(".main-nav");

if (menuButton && menu) {
  menuButton.addEventListener("click", () => {
    const isOpen = menu.classList.toggle("is-open");
    menuButton.setAttribute("aria-expanded", String(isOpen));
  });
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

  videoModal.classList.remove("is-open");
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
  videoModal.querySelector(".video-modal__close")?.focus();
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
    previous: "Назад", next: "Продолжить", finish: "Зарегистрироваться", required: "Заполните обязательные поля.", mismatch: "Пароли не совпадают.", success: "Вы успешно создали аккаунт!",
  },
  en: {
    back: "← Back to sign in", title: "Create a new account", lead: "Fill in the details to register with RaceManager.",
    step1: "Step 1. Account details", step2: "Step 2. Security", step3: "Step 3. Driver details",
    login: "Enter login", email: "Enter email", password: "Enter password", confirmPassword: "Repeat password",
    lastName: "Last name", firstName: "First name", middleName: "Middle name", phone: "Phone number", car: "Car (optional)",
    loginHint: "Login must contain at least 4 characters", passwordHint: "Password must contain at least 6 characters", carHint: "You can add a car later in your personal account.",
    previous: "Back", next: "Continue", finish: "Register", required: "Complete all required fields.", mismatch: "Passwords do not match.", success: "You have successfully created an account!",
  },
  de: {
    back: "← Zurück zur Anmeldung", title: "Neues Konto erstellen", lead: "Füllen Sie die Daten für die RaceManager-Registrierung aus.",
    step1: "Schritt 1. Kontodaten", step2: "Schritt 2. Sicherheit", step3: "Schritt 3. Fahrerdaten",
    login: "Login eingeben", email: "E-Mail eingeben", password: "Passwort eingeben", confirmPassword: "Passwort wiederholen",
    lastName: "Nachname", firstName: "Vorname", middleName: "Vatersname", phone: "Telefonnummer", car: "Fahrzeug (optional)",
    loginHint: "Der Login muss mindestens 4 Zeichen enthalten", passwordHint: "Das Passwort muss mindestens 6 Zeichen enthalten", carHint: "Sie können das Fahrzeug später im persönlichen Konto hinzufügen.",
    previous: "Zurück", next: "Weiter", finish: "Registrieren", required: "Füllen Sie alle Pflichtfelder aus.", mismatch: "Die Passwörter stimmen nicht überein.", success: "Sie haben erfolgreich ein Konto erstellt!",
  },
};

function getRegisterLanguage() {
  return ["ru", "en", "de"].includes(document.documentElement.lang) ? document.documentElement.lang : "ru";
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

function validateRegisterStep(step) {
  const translations = registerTranslations[getRegisterLanguage()];
  const stepElement = document.querySelector(`[data-register-step="${step}"]`);
  const requiredFields = stepElement?.querySelectorAll("[required]") || [];
  const valid = [...requiredFields].every((field) => field.checkValidity());
  if (!valid) {
    if (registerError) { registerError.textContent = translations.required; registerError.hidden = false; }
    return false;
  }
  if (step === 2) {
    const password = registerForm?.elements.registerPassword?.value;
    const confirmPassword = registerForm?.elements.confirmPassword?.value;
    if (password !== confirmPassword) {
      if (registerError) { registerError.textContent = translations.mismatch; registerError.hidden = false; }
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
      window.location.href = "login.html";
      return;
    }
    showRegisterStep(currentRegisterStep - 1);
  });
  registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!validateRegisterStep(3)) return;

    const translations = registerTranslations[getRegisterLanguage()];
    const email = registerForm.elements.email.value.trim().toLowerCase();
    const registeredUsers = getRegisteredUsers();
    const seedUsers = await getSeedUsers();
    const emailExists = [...seedUsers, ...registeredUsers].some((user) => user.email.toLowerCase() === email);

    if (emailExists) {
      if (registerError) { registerError.textContent = "Пользователь с таким email уже существует."; registerError.hidden = false; }
      return;
    }

    const user = {
      id: crypto.randomUUID ? crypto.randomUUID() : "user-" + Date.now(),
      login: registerForm.elements.login.value.trim(),
      email,
      password: registerForm.elements.registerPassword.value,
      role: "Пользователь",
      statistics: { ranking: 1, points: 0, races: 0, wins: 0, podiums: 0, qualifications: 0, bestResult: "—", finishedEvents: 0 },
      championships: [],
      team: null,
      vehicles: [],
      applications: [],
      avatar: "",
      profile: {
        lastName: registerForm.elements.lastName.value.trim(),
        firstName: registerForm.elements.firstName.value.trim(),
        middleName: registerForm.elements.middleName.value.trim(),
        phone: registerForm.elements.phone.value.trim(),
        birthDate: registerForm.elements.birthDate.value,
        car: registerForm.elements.car.value.trim(),
      },
    };

    registeredUsers.push(user);
    saveRegisteredUsers(registeredUsers);
    const sessionUser = { id: user.id, login: user.login, email: user.email, role: user.role, profile: user.profile, statistics: user.statistics, championships: user.championships, team: user.team, vehicles: user.vehicles || [], applications: user.applications || [], avatar: user.avatar || "" };
    localStorage.setItem(currentUserStorageKey, JSON.stringify(sessionUser));
    if (registerError) registerError.hidden = true;
    showAuthToast(translations.success);
    registerForm.reset();
    setTimeout(() => { window.location.href = "account.html"; }, 3000);
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

const authForm = document.querySelector(".auth-form");
const authMessage = document.querySelector("[data-auth-message]");
const authToast = document.querySelector("[data-auth-toast]");
let authToastTimer = null;
const registeredUsersStorageKey = "racemanagerRegisteredUsers";
const currentUserStorageKey = "racemanagerCurrentUser";
const userOverridesStorageKey = "racemanagerUserOverrides";

function getRegisteredUsers() {
  try {
    return JSON.parse(localStorage.getItem(registeredUsersStorageKey) || "[]");
  } catch (error) {
    return [];
  }
}

function saveRegisteredUsers(users) {
  localStorage.setItem(registeredUsersStorageKey, JSON.stringify(users));
}

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
    const response = await fetch("data/users.json");
    if (!response.ok) return [];
    const data = await response.json();
    return Array.isArray(data.users) ? data.users : [];
  } catch (error) {
    return [];
  }
}

if (authForm) {
  authForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const email = authForm.elements.email.value.trim().toLowerCase();
    const password = authForm.elements.password.value;
    const overrides = getUserOverrides();
    const users = [...await getSeedUsers(), ...getRegisteredUsers()].map((item) => ({ ...item, ...(overrides[item.id] || {}) }));
    const baseUser = users.find((item) => item.email.toLowerCase() === email && item.password === password);

    if (!baseUser) {
      showAuthMessage("Неверный email или пароль.");
      return;
    }

    const user = baseUser;
    const sessionUser = { id: user.id, login: user.login, email: user.email, role: user.role, profile: user.profile, statistics: user.statistics, championships: user.championships, team: user.team, vehicles: user.vehicles || [], applications: user.applications || [], avatar: user.avatar || "" };
    localStorage.setItem(currentUserStorageKey, JSON.stringify(sessionUser));
    if (authMessage) authMessage.hidden = true;
    const translations = authTranslations[document.documentElement.lang] || authTranslations.ru;
    showAuthToast(translations.loginSuccess);
    setTimeout(() => { window.location.href = "account.html"; }, 3000);
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

function getUserOverrides() {
  try { return JSON.parse(localStorage.getItem(userOverridesStorageKey) || "{}"); } catch (error) { return {}; }
}

function saveCurrentUser(user) {
  localStorage.setItem(currentUserStorageKey, JSON.stringify(user));
  const overrides = getUserOverrides();
  overrides[user.id] = user;
  localStorage.setItem(userOverridesStorageKey, JSON.stringify(overrides));

  const registeredUsers = getRegisteredUsers();
  const index = registeredUsers.findIndex((item) => item.id === user.id);
  if (index !== -1) {
    registeredUsers[index] = { ...registeredUsers[index], ...user };
    saveRegisteredUsers(registeredUsers);
  }
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

function renderAccountTeam(team) {
  const container = document.querySelector("[data-account-team]");
  if (!container || !team) return;
  container.className = "account-team-card";
  container.style.setProperty("--team-color", team.color || "#e10600");
  container.innerHTML = `<div class="account-team-card__logo"><img src="${team.logo}" alt="Логотип ${team.name}"></div><div class="account-team-card__copy"><small>${team.role}</small><h4>${team.name}</h4><p>Команда участвует в командном зачёте текущего сезона.</p></div><dl><div><dt>Позиция</dt><dd>${team.position}</dd></div><div><dt>Очки</dt><dd>${team.points}</dd></div></dl><a href="${team.page}">Страница команды →</a>`;
}

if (accountPage) {
  const user = getCurrentUser();
  accountUser = user;
  if (!user) {
    window.location.href = "login.html";
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
    setAccountText("[data-account-birth-date]", profile.birthDate);
    setAccountText("[data-account-car]", profile.car || "Автомобиль не указан");
    setAccountText("[data-account-team-name]", user.team?.name || "Не состоит в команде");
    renderAccountStatistics(user.statistics);
    renderAccountChampionships(user.championships);
    renderAccountTeam(user.team);
    renderAccountVehicles(user.vehicles || []);
    renderAccountApplications(user.applications || []);
    populateAccountSettings(user);
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
  if (!grid || !vehicles.length) return;
  grid.replaceChildren();
  vehicles.forEach((vehicle) => {
    const card = document.createElement("button");
    card.type = "button";
    card.className = "account-vehicle-card";
    card.innerHTML = `<img src="${vehicle.image}" alt="${vehicle.name}"><div><small>${vehicleValue(vehicle.type)}</small><h3>${vehicle.name}</h3><p>${vehicleValue(vehicle.power, "л.с.")} · ${vehicle.drive ? vehicle.drive + " привод" : "Привод не указан"}</p></div>`;
    card.addEventListener("click", () => openVehicleDetails(vehicle));
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
    card.innerHTML = `<div class="account-application-card__date">${application.date}</div><div class="account-application-card__name"><strong>${application.name}</strong><span>${application.location}</span></div><div class="account-application-card__type">${application.type}</div><a href="${application.href || "calendar.html"}">Подробнее <span>›</span></a>`;
    container.append(card);
  });
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
}

function openAccountModal(modal) {
  if (!modal) return;
  modal.classList.add("is-open");
  modal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
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
  content.innerHTML = `<img class="vehicle-details__image" src="${vehicle.image}" alt="${vehicle.name}"><div class="account-modal__head"><span class="eyebrow">${vehicleValue(vehicle.type)}</span><h2>${vehicle.name}</h2></div><dl class="vehicle-details__grid"><div><dt>Мощность</dt><dd>${vehicleValue(vehicle.power, "л.с.")}</dd></div><div><dt>Вес</dt><dd>${vehicleValue(vehicle.weight, "кг")}</dd></div><div><dt>Удельная мощность</dt><dd>${vehicleValue(vehicle.powerToWeight, "л.с./т.")}</dd></div><div><dt>Привод</dt><dd>${vehicleValue(vehicle.drive)}</dd></div><div><dt>Тип двигателя</dt><dd>${vehicleValue(vehicle.engineType)}</dd></div><div><dt>Модель двигателя</dt><dd>${vehicleValue(vehicle.engineModel)}</dd></div><div><dt>Объем</dt><dd>${vehicleValue(vehicle.engineVolume, "см³")}</dd></div><div><dt>Крутящий момент</dt><dd>${vehicleValue(vehicle.torque, "Нм")}</dd></div></dl>`;
  openAccountModal(modal);
}

document.querySelectorAll("[data-account-tab]").forEach((tab) => {
  tab.addEventListener("click", () => {
    const selected = tab.dataset.accountTab;
    document.querySelectorAll("[data-account-tab]").forEach((button) => button.classList.toggle("is-active", button === tab));
    document.querySelectorAll("[data-account-view]").forEach((view) => { view.hidden = view.dataset.accountView !== selected; });
  });
});

const vehicleModal = document.querySelector("[data-vehicle-modal]");
document.querySelectorAll("[data-open-vehicle-modal]").forEach((button) => button.addEventListener("click", () => openAccountModal(vehicleModal)));
document.querySelectorAll("[data-close-vehicle-modal]").forEach((button) => button.addEventListener("click", () => closeAccountModal(vehicleModal)));
document.querySelectorAll("[data-close-vehicle-details]").forEach((button) => button.addEventListener("click", () => closeAccountModal(document.querySelector("[data-vehicle-details-modal]"))));

document.querySelector("[data-vehicle-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!accountUser) return;
  const form = event.currentTarget;
  const image = await readImageFile(form.elements.image.files[0]);
  const optionalValue = (value) => value.trim() || "";
  const vehicle = { id: crypto.randomUUID ? crypto.randomUUID() : "vehicle-" + Date.now(), image, name: form.elements.name.value.trim(), type: optionalValue(form.elements.type.value), power: optionalValue(form.elements.power.value), weight: optionalValue(form.elements.weight.value), powerToWeight: optionalValue(form.elements.powerToWeight.value), drive: optionalValue(form.elements.drive.value), engineType: optionalValue(form.elements.engineType.value), engineModel: optionalValue(form.elements.engineModel.value), engineVolume: optionalValue(form.elements.engineVolume.value), torque: optionalValue(form.elements.torque.value) };
  accountUser.vehicles = [...(accountUser.vehicles || []), vehicle];
  accountUser.profile = { ...(accountUser.profile || {}), car: accountUser.profile?.car || vehicle.name };
  try {
    saveCurrentUser(accountUser);
  } catch (error) {
    accountUser.vehicles = accountUser.vehicles.filter((item) => item.id !== vehicle.id);
    window.alert("Не удалось сохранить автомобиль. Попробуйте загрузить изображение меньшего размера.");
    return;
  }
  renderAccountVehicles(accountUser.vehicles);
  setAccountText("[data-account-car]", accountUser.profile.car);
  form.reset();
  closeAccountModal(vehicleModal);
});

document.querySelector("[data-account-settings-form]")?.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!accountUser) return;
  const form = event.currentTarget;
  const avatarFile = form.elements.avatar.files[0];
  if (avatarFile) accountUser.avatar = await readImageFile(avatarFile);
  accountUser.email = form.elements.email.value.trim().toLowerCase();
  accountUser.profile = { ...(accountUser.profile || {}), lastName: form.elements.lastName.value.trim(), firstName: form.elements.firstName.value.trim(), middleName: form.elements.middleName.value.trim(), phone: form.elements.phone.value.trim() };
  saveCurrentUser(accountUser);
  const fullName = [accountUser.profile.lastName, accountUser.profile.firstName, accountUser.profile.middleName].filter(Boolean).join(" ");
  setAccountText("[data-account-full-name]", fullName);
  setAccountText("[data-account-email]", accountUser.email);
  setAccountText("[data-account-last-name]", accountUser.profile.lastName);
  setAccountText("[data-account-first-name]", accountUser.profile.firstName);
  setAccountText("[data-account-middle-name]", accountUser.profile.middleName);
  setAccountText("[data-account-phone]", accountUser.profile.phone);
  renderAccountAvatar(accountUser.avatar);
  const message = document.querySelector("[data-settings-message]");
  if (message) { message.textContent = "Изменения сохранены"; message.hidden = false; setTimeout(() => { message.hidden = true; }, 3000); }
});

document.querySelector("[data-account-logout]")?.addEventListener("click", () => {
  localStorage.removeItem(currentUserStorageKey);
  window.location.href = "login.html";
});


function getUserInitials(user) {
  const profile = user?.profile || {};
  return [profile.firstName, profile.lastName].filter(Boolean).map((part) => part[0]).join("").toUpperCase() || (user?.login || "RM").slice(0, 2).toUpperCase();
}

document.querySelectorAll('a.button[href="login.html"]').forEach((link) => {
  const user = getCurrentUser();
  if (!user) return;
  const initials = getUserInitials(user);
  link.href = "account.html";
  link.className = "header-avatar-link";
  link.setAttribute("aria-label", "Открыть личный кабинет");
  link.innerHTML = "<span>" + initials + "</span>";
  if (user.avatar) {
    const avatar = link.querySelector("span");
    avatar.textContent = "";
    avatar.style.backgroundImage = "url(\"" + user.avatar + "\")";
  }
});

document.querySelectorAll(".calendar-event__primary").forEach((button) => {
  const action = button.textContent.trim().toLowerCase();
  const isRegistrationAction = action.includes("зарегистр") || action.includes("записаться");
  if (!isRegistrationAction) return;

  button.addEventListener("click", (event) => {
    event.preventDefault();
    const user = getCurrentUser();
    if (!user) {
      window.location.href = "login.html";
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
const dragParticipantsStorageKey = "racemanager.drag402.stage2.participants";

function getDragParticipants() {
  try {
    return JSON.parse(localStorage.getItem(dragParticipantsStorageKey) || "[]");
  } catch (error) {
    return [];
  }
}

function saveDragParticipants(participants) {
  localStorage.setItem(dragParticipantsStorageKey, JSON.stringify(participants));
}

function renderDragParticipants() {
  if (!dragParticipantsBody) return;
  const participants = getDragParticipants();
  if (!participants.length) {
    dragParticipantsBody.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="5">Пока нет зарегистрированных участников.</td></tr>';
    return;
  }
  dragParticipantsBody.innerHTML = participants.map((pilot, index) => `
    <tr>
      <td>${index + 1}</td>
      <td>${pilot.fullName}</td>
      <td>${pilot.email}</td>
      <td>${pilot.phone}</td>
      <td>${pilot.car}</td>
    </tr>
  `).join("");
}

function openDragRegisterModal() {
  if (!dragRegisterModal) return;
  dragRegisterModal.classList.add("is-open");
  dragRegisterModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
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

dragRegisterForm?.addEventListener("submit", (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const participant = {
    id: `drag402-${Date.now()}`,
    fullName: form.elements.fullName.value.trim(),
    email: form.elements.email.value.trim().toLowerCase(),
    phone: form.elements.phone.value.trim(),
    car: form.elements.car.value.trim(),
  };
  const participants = getDragParticipants();
  participants.push(participant);
  saveDragParticipants(participants);
  renderDragParticipants();
  form.reset();
  closeDragRegisterModal();
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && dragRegisterModal?.classList.contains("is-open")) closeDragRegisterModal();
});

const organizerEventsStorageKey = "racemanager.organizer.events";

function getOrganizerEvents() {
  try { return JSON.parse(localStorage.getItem(organizerEventsStorageKey) || "[]"); } catch (error) { return []; }
}

function saveOrganizerEvents(events) {
  localStorage.setItem(organizerEventsStorageKey, JSON.stringify(events));
}

function eventTypeLabel(type) {
  return type === "Чемпионат" ? "чемпионат" : type.toLowerCase();
}

function setupRoleUi(user) {
  if (!user) return;
  const isPrivileged = user.role === "Организатор" || user.role === "Судья";
  document.querySelectorAll("[data-account-role-badge], [data-account-profile-role-badge]").forEach((badge) => {
    badge.hidden = !isPrivileged;
    badge.textContent = user.role || "Пользователь";
    badge.classList.toggle("account-role-badge--judge", user.role === "Судья");
    badge.classList.toggle("account-role-badge--organizer", user.role === "Организатор");
  });
  document.querySelectorAll("[data-organizer-only]").forEach((element) => {
    element.hidden = user.role !== "Организатор";
  });
  document.querySelectorAll("[data-judge-only]").forEach((element) => {
    element.hidden = user.role !== "Судья";
  });
}

setupRoleUi(getCurrentUser());

const createEventForm = document.querySelector("[data-create-event-form]");
const createEventPage = document.querySelector(".create-event-page");

if (createEventPage) {
  const user = getCurrentUser();
  if (!user || user.role !== "Организатор") window.location.href = "account.html";
}

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

function updateCreateEventFields() {
  const type = document.querySelector("[data-event-type]")?.value || "Чемпионат";
  const discipline = document.querySelector("[data-event-discipline]")?.value || "Дрифт";
  const stages = document.querySelector("[data-championship-stages]");
  const submitLabel = document.querySelector("[data-create-submit-label]");
  const distanceLabel = document.querySelector("[data-discipline-distance-label]");
  if (stages) stages.hidden = type !== "Чемпионат";
  if (submitLabel) submitLabel.textContent = eventTypeLabel(type);
  if (distanceLabel) distanceLabel.textContent = discipline === "Тайм-Аттак" ? "Количество кругов" : "Дистанция, м";
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
  const stages = stageNames.map((name, index) => ({ name: name || `${index + 1} этап`, date: stageDates[index] || "", intro: stageIntros[index] || "", registrationStatus: stageStatuses[index] || "Скоро" })).filter((stage) => stage.name || stage.date || stage.intro);
  const eventImage = await readOptionalImage(form.elements.eventImage);
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
    stages,
    standings: { pilots: [], teams: [] },
  };
  const events = getOrganizerEvents();
  events.unshift(createdEvent);
  saveOrganizerEvents(events);
  const message = document.querySelector("[data-create-event-message]");
  if (message) { message.hidden = false; message.textContent = `${createdEvent.type} создан. Событие добавлено в календарь.`; }
  setTimeout(() => { window.location.href = `CreatedEvent.html?id=${createdEvent.id}`; }, 1000);
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
  const block = document.createElement("section");
  block.className = "calendar-series-block calendar-series-block--created";
  block.innerHTML = `<div class="calendar-series-cover calendar-series-cover--created"><div class="calendar-series-cover__shade"></div><div class="calendar-series-cover__content"><a class="button button--white button--small calendar-series-back" href="CreateEvent.html">+ Создать</a><div><span>Создано организаторами</span><h2>Новые события</h2><p>События, созданные через личный кабинет организатора.</p></div><a class="button button--glass button--small calendar-series-rating" href="account.html">Кабинет</a></div></div><div class="calendar-series-list"></div>`;
  const list = block.querySelector(".calendar-series-list");
  events.forEach((item, index) => {
    const article = document.createElement("article");
    article.className = "calendar-series-event calendar-series-event--open";
    article.dataset.stage = String(index + 1).padStart(2, "0");
    article.innerHTML = `<img src="${item.eventImage || "public/drag402banner.jpg"}" alt="${item.name}"><div class="calendar-series-event__body"><h3>${item.name}</h3><p>${item.track} · ${item.discipline}</p><time>${formatEventDate(item.eventDate)}</time><small>${item.trackConfig ? "Конфигурация трека загружена" : "Конфигурация трека не найдена."}</small></div><a class="button button--red button--small calendar-series-event__button" href="CreatedEvent.html?id=${item.id}">Подробнее</a>`;
    list.append(article);
  });
  stack.prepend(block);
}

renderOrganizerCalendarEvents();

function getEventById(eventId) {
  return getOrganizerEvents().find((item) => item.id === eventId) || null;
}

function createdEventParticipantsKey(eventId) {
  return `racemanager.createdEvent.${eventId}.participants`;
}

function getCreatedEventParticipants(eventId) {
  try { return JSON.parse(localStorage.getItem(createdEventParticipantsKey(eventId)) || "[]"); } catch (error) { return []; }
}

function saveCreatedEventParticipants(eventId, participants) {
  localStorage.setItem(createdEventParticipantsKey(eventId), JSON.stringify(participants));
}

function renderCreatedEventParticipants(eventId) {
  const body = document.querySelector("[data-created-event-participants-list]");
  if (!body) return;
  const participants = getCreatedEventParticipants(eventId);
  if (!participants.length) {
    body.innerHTML = '<tr class="drag-entry-table__empty"><td colspan="5">Пока нет зарегистрированных участников.</td></tr>';
    return;
  }
  body.innerHTML = participants.map((pilot, index) => `<tr><td>${index + 1}</td><td>${pilot.fullName}</td><td>${pilot.email}</td><td>${pilot.phone}</td><td>${pilot.car}</td></tr>`).join("");
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

function initCreatedEventPage() {
  const page = document.querySelector("[data-created-event-page]");
  if (!page) return;
  const eventId = new URLSearchParams(window.location.search).get("id");
  const event = getEventById(eventId);
  if (!event) {
    page.innerHTML = '<section class="section"><div class="container"><div class="account-empty-state account-empty-state--large"><strong>Событие не найдено</strong><p>Вернитесь в календарь и выберите существующее событие.</p><a class="button button--red button--small" href="calendar.html">К календарю</a></div></div></section>';
    return;
  }

  const image = document.querySelector("[data-created-event-image]");
  if (image) image.src = event.eventImage || "public/drag402banner.jpg";
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
    stages.innerHTML = eventStages.length ? eventStages.map((stage, index) => `<article><b>${String(index + 1).padStart(2, "0")}</b><div><h3>${stage.name}</h3><time>${formatEventDate(stage.date)}</time><p>${stage.intro || "Вводная информация не указана."}</p></div><span>${stage.registrationStatus || "Скоро"}</span></article>`).join("") : '<p>Этапы пока не указаны.</p>';
  }

  const standingsSection = document.querySelector("[data-created-event-standings-section]");
  if (standingsSection) standingsSection.hidden = event.type !== "Чемпионат";
  renderCreatedEventStandings(event);
  renderCreatedEventParticipants(event.id);

  const modal = document.querySelector(".drag-register-modal");
  document.querySelector("[data-created-event-register-open]")?.addEventListener("click", () => openAccountModal(modal));
  document.querySelectorAll("[data-created-event-register-close]").forEach((button) => button.addEventListener("click", () => closeAccountModal(modal)));
  document.querySelector("[data-created-event-register-form]")?.addEventListener("submit", (submitEvent) => {
    submitEvent.preventDefault();
    const form = submitEvent.currentTarget;
    const participant = { id: `participant-${Date.now()}`, fullName: form.elements.fullName.value.trim(), email: form.elements.email.value.trim().toLowerCase(), phone: form.elements.phone.value.trim(), car: form.elements.car.value.trim() };
    const participants = getCreatedEventParticipants(event.id);
    participants.push(participant);
    saveCreatedEventParticipants(event.id, participants);
    renderCreatedEventParticipants(event.id);
    form.reset();
    closeAccountModal(modal);
  });
}

initCreatedEventPage();

const judgeResultsStorageKey = "racemanager.judge.results";

function getJudgeResults() {
  try { return JSON.parse(localStorage.getItem(judgeResultsStorageKey) || "{}"); } catch (error) { return {}; }
}

function saveJudgeResults(results) {
  localStorage.setItem(judgeResultsStorageKey, JSON.stringify(results));
}

function getJudgeEvents() {
  const staticEvents = [
    { id: "drag402-stage-2", name: "Drag402 Кубок Городов · 2 этап", type: "Чемпионат", date: "2026-06-20", track: "Аэропорт Могилев", participantsKey: dragParticipantsStorageKey },
    { id: "betera-grodno", name: "Betera Гран-При Гродно", type: "Чемпионат", date: "2026-06-27", track: "Гродно", participantsKey: "racemanager.betera.grodno.participants" }
  ];
  const created = getOrganizerEvents().map((event) => ({ id: event.id, name: event.name, type: event.type, date: event.eventDate, track: event.track, created: true }));
  return [...staticEvents, ...created];
}

function getJudgeParticipants(eventId) {
  if (!eventId) return [];
  if (eventId === "drag402-stage-2") return getDragParticipants();
  const created = getEventById(eventId);
  if (created) return getCreatedEventParticipants(eventId);
  try { return JSON.parse(localStorage.getItem(`racemanager.${eventId}.participants`) || "[]"); } catch (error) { return []; }
}

function renderJudgeEvents() {
  const select = document.querySelector("[data-judge-event-select]");
  if (!select) return;
  const events = getJudgeEvents();
  select.innerHTML = events.map((event) => `<option value="${event.id}">${event.name} · ${formatEventDate(event.date)}</option>`).join("");
  renderJudgeParticipants();
  renderJudgeResults();
}

function renderJudgeParticipants() {
  const eventSelect = document.querySelector("[data-judge-event-select]");
  const participantSelect = document.querySelector("[data-judge-participant-select]");
  if (!eventSelect || !participantSelect) return;
  const participants = getJudgeParticipants(eventSelect.value);
  participantSelect.innerHTML = '<option value="">Ввести вручную</option>' + participants.map((pilot) => `<option value="${pilot.id || pilot.email || pilot.fullName}">${pilot.fullName} · ${pilot.car || "автомобиль не указан"}</option>`).join("");
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
  if (!rows.length) {
    body.innerHTML = '<tr><td colspan="6">Результаты пока не внесены.</td></tr>';
    return;
  }
  body.innerHTML = rows
    .slice()
    .sort((a, b) => (Number(a.position) || 999) - (Number(b.position) || 999))
    .map((row) => `<tr data-judge-result-id="${row.id}"><td>${row.position || "—"}</td><td>${row.pilotName}</td><td>${row.lapTime || "—"}</td><td>${row.bestLap || "—"}</td><td>${row.penalty || "—"}</td><td><span class="judge-status ${row.status === "Дисквалификация" ? "judge-status--dsq" : ""}">${row.status}</span></td></tr>`)
    .join("");
}

function fillJudgeForm(result) {
  const form = document.querySelector("[data-judge-result-form]");
  if (!form || !result) return;
  form.elements.pilotName.value = result.pilotName || "";
  form.elements.position.value = result.position || "";
  form.elements.lapTime.value = result.lapTime || "";
  form.elements.bestLap.value = result.bestLap || "";
  form.elements.penalty.value = result.penalty || "";
  form.elements.status.value = result.status || "Финишировал";
  form.elements.comment.value = result.comment || "";
  form.dataset.editingResultId = result.id;
}

renderJudgeEvents();

document.querySelector("[data-judge-event-select]")?.addEventListener("change", () => {
  renderJudgeParticipants();
  renderJudgeResults();
});

document.querySelector("[data-judge-participant-select]")?.addEventListener("change", (event) => {
  const participant = getJudgeParticipants(document.querySelector("[data-judge-event-select]")?.value).find((item) => String(item.id || item.email || item.fullName) === event.currentTarget.value);
  const form = document.querySelector("[data-judge-result-form]");
  if (participant && form) form.elements.pilotName.value = participant.fullName;
});

document.querySelector("[data-judge-result-form]")?.addEventListener("submit", (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const eventId = form.elements.eventId.value;
  const store = getJudgeResults();
  const rows = store[eventId] || [];
  const id = form.dataset.editingResultId || `result-${Date.now()}`;
  const result = {
    id,
    pilotName: form.elements.pilotName.value.trim(),
    position: form.elements.position.value,
    lapTime: form.elements.lapTime.value.trim(),
    bestLap: form.elements.bestLap.value.trim(),
    penalty: form.elements.penalty.value.trim(),
    status: form.elements.status.value,
    comment: form.elements.comment.value.trim(),
    updatedAt: new Date().toISOString(),
  };
  const index = rows.findIndex((item) => item.id === id || item.pilotName.toLowerCase() === result.pilotName.toLowerCase());
  if (index === -1) rows.push(result);
  else rows[index] = { ...rows[index], ...result };
  store[eventId] = rows;
  saveJudgeResults(store);
  form.removeAttribute("data-editing-result-id");
  form.reset();
  renderJudgeParticipants();
  renderJudgeResults();
  const message = document.querySelector("[data-judge-save-message]");
  if (message) { message.hidden = false; message.textContent = "Результат сохранен"; setTimeout(() => { message.hidden = true; }, 2500); }
});

document.querySelector("[data-judge-results]")?.addEventListener("click", (event) => {
  const row = event.target.closest("[data-judge-result-id]");
  if (!row) return;
  const { rows } = getSelectedJudgeResults();
  fillJudgeForm(rows.find((item) => item.id === row.dataset.judgeResultId));
});
