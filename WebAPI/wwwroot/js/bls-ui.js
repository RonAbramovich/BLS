/**
 * Alpine.js component for the BLS single-signer flow.
 * Used by index.html (Hebrew) and index-en.html (English).
 */
function blsApp(lang) {
  const isHe = lang === 'he';

  return {
    /* --- curve params --- */
    q: '7919',
    a: '15',
    b: '42',

    /* --- private key --- */
    keyMode: 'select',
    privateKeySelect: '',
    privateKeyManual: '',
    validKeys: [],

    /* --- message --- */
    message: isHe ? 'שלום' : 'hello',

    /* --- options --- */
    includeDetailedReport: true,

    /* --- state --- */
    constraints: null,
    result: null,
    loading: false,
    error: null,
    activeTab: 'steps',
    lang: lang,

    get selectedPrivateKey() {
      return this.keyMode === 'select' ? this.privateKeySelect : this.privateKeyManual;
    },

    /* ---- Step 1 → 2: Validate params & show constraints --------------- */
    async showConstraints() {
      this.loading = true;
      this.error = null;
      this.constraints = null;
      this.result = null;

      try {
        const res = await fetch('/api/bls/validate-parameters', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            PRIME_Q: this.q,
            A: this.a,
            B: this.b,
            Language: this.lang
          })
        });
        const data = await res.json();
        if (!data.success) throw new Error(data.errorMessage || (isHe ? 'שגיאה לא ידועה' : 'Unknown error'));

        this.constraints = data;
        this.validKeys = data.validKeyExamples || [];
        this.privateKeySelect = '';

        this.$nextTick(() => {
          this.$refs.constraintsSection?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
      } catch (e) {
        this.error = e.message;
      } finally {
        this.loading = false;
      }
    },

    /* ---- Step 3: Compute signature ------------------------------------ */
    async generateSignature() {
      this.loading = true;
      this.error = null;
      this.result = null;

      try {
        const res = await fetch('/api/bls/sign', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            q: this.q,
            a: this.a,
            b: this.b,
            privateKey: this.selectedPrivateKey,
            message: this.message,
            language: this.lang,
            includeDetailedReport: this.includeDetailedReport
          })
        });
        const data = await res.json();
        if (!data.success) throw new Error(data.errorMessage || (isHe ? 'שגיאה לא ידועה' : 'Unknown error'));

        this.result = data;
        this.activeTab = 'steps';

        this.$nextTick(() => {
          this.renderKaTeX();
          this.$refs.resultsSection?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
      } catch (e) {
        this.error = e.message;
      } finally {
        this.loading = false;
      }
    },

    /* ---- KaTeX rendering ---------------------------------------------- */
    renderKaTeX() {
      if (typeof katex === 'undefined') return;

      document.querySelectorAll('[data-katex]').forEach(el => {
        const tex = el.getAttribute('data-katex');
        if (tex && !el._katexRendered) {
          katex.render(tex, el, { displayMode: false, throwOnError: false });
          el._katexRendered = true;
        }
      });
    },

    /* ---- Lifecycle ----------------------------------------------------- */
    init() {
      this.$nextTick(() => this.renderKaTeX());
    }
  };
}
