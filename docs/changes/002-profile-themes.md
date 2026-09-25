# 002 — My profile, Tailwind and themes

User request: adopt Tailwind alongside SCSS, add user-specific profile preferences and light/dark modes.

Implemented Tailwind 4.3.3 using Angular CLI integration with a separate src/tailwind.css and PostCSS entry; existing component SCSS remains for dashboard styling. Shared semantic CSS variables cover surfaces, text, borders, alerts and charts in both themes. No backend/API/database changes were needed.

My profile uses Angular Signal Forms for display name, preferred tracked player tag and default 7/14/90-day history range. Saved settings are browser-local, versioned under clash-insights.preferences.v1. They are not an account or ownership verification and do not change server tracking. The header provides a quick light/dark toggle; the profile also supports device preference, immediate changes and reset. Malformed or unavailable browser storage is handled without crashing. Initial appearance is applied before Angular loads.

Verification: production build passed without budget warnings; 10 frontend unit tests; profile browser tests verify reload persistence, preferred player/range, light/dark/device theme, mobile overflow, axe checks across all dark sections, validation, and API/storage failure states. Existing dashboard browser/API checks also retained. Captured desktop dark profile and mobile light profile were visually inspected.

Limit: preferences belong to this browser and origin; clearing browser storage resets them. Hosted accounts remain v2. No secrets are stored in the profile.
