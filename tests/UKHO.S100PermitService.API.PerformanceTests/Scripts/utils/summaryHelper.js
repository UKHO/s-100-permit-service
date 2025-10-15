// Utility helpers reused by performance test scripts.

/**
 * Build a compact timestamp from an ISO string:
 * Example: 2024-10-06T12:34:56 => 20241006_123456
 */
export function buildTimestamp(date = new Date()) {
  return date
    .toISOString()        // e.g. 2024-10-06T12:34:56.123Z
    .slice(0, 19)         // trim milliseconds + Z => 2024-10-06T12:34:56
    .replace('T', '_')    // separator
    .replaceAll(':', '')  // remove colons
    .replaceAll('-', ''); // remove dashes
}

/**
 * Create k6 handleSummary output object.
 * @param {object} data - k6 summary data passed to handleSummary.
 * @param {string} testPrefix - Short test name prefix for filenames.
 * @param {function} htmlReport - Reporter fn (injected so this stays pure).
 * @param {function} textSummary - Text summary fn.
 */
export function createSummaryArtifacts(data, testPrefix, htmlReport, textSummary) {
  const ts = buildTimestamp();
  const base = `./../Summary/${testPrefix}${ts}`;
  return {
    [`${base}.html`]: htmlReport(data),                  // computed property name
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    [`${base}.json`]: JSON.stringify(data),
  };
}