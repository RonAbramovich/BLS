/**
 * Alpine.js component for the BLS aggregated (multi-signer) flow.
 * Used by aggregated.html (Hebrew) and aggregated-en.html (English).
 */
function aggregatedApp(lang) {
  const isHe = lang === 'he';

  return {
    /* --- curve params --- */
    q: '43',
    a: '1',
    b: '8',

    /* --- message --- */
    message: isHe ? 'שלום' : 'hello',

    /* --- participants --- */
    participants: [],
    nextId: 0,

    /* --- state --- */
    constraints: null,
    result: null,
    loading: false,
    error: null,
    validKeys: [],
    lang: lang,

    get canSubmit() {
      return this.participants.length >= 2;
    },

    addParticipant() {
      this.nextId++;
      this.participants.push({ id: this.nextId, name: '', key: '' });
    },

    removeParticipant(id) {
      this.participants = this.participants.filter(p => p.id !== id);
    },

    fillDemoParticipants() {
      const names = isHe
        ? ['אליס', 'בוב', 'צ\'ארלי', 'דיאנה', 'איב', 'פרנק', 'גרייס']
        : ['Alice', 'Bob', 'Charlie', 'Diana', 'Eve', 'Frank', 'Grace'];

      const keys = [...this.validKeys].map(String);
      for (let i = keys.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [keys[i], keys[j]] = [keys[j], keys[i]];
      }

      this.participants = names.map((name, i) => ({
        id: i + 1,
        name,
        key: ''
      }));
      this.nextId = names.length;

      this.$nextTick(() => {
        this.participants.forEach((p, i) => {
          p.key = keys[i % keys.length];
        });
      });
    },

    getInitials(name) {
      if (!name) return '?';
      return name.trim().charAt(0).toUpperCase();
    },

    /* ---- Step 1 → 2: Validate params & show constraints --------------- */
    async showConstraints() {
      this.loading = true;
      this.error = null;
      this.constraints = null;
      this.result = null;
      this.participants = [];
      this.nextId = 0;

      try {
        const res = await fetch('/api/aggregated/private-key-constraints', {
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

        this.addParticipant();
        this.addParticipant();

        this.$nextTick(() => {
          this.$refs.constraintsSection?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
      } catch (e) {
        this.error = e.message;
      } finally {
        this.loading = false;
      }
    },

    /* ---- Step 3: Compute aggregated signature ------------------------- */
    async generateAggregatedSignature() {
      const errPrefix = isHe ? 'שגיאה: ' : 'Error: ';

      for (const p of this.participants) {
        if (!p.name.trim()) {
          this.error = errPrefix + (isHe ? 'יש להזין שם לכל המשתתפים' : 'Please enter a name for all participants');
          return;
        }
        if (!p.key) {
          this.error = errPrefix + (isHe ? 'יש לבחור מפתח פרטי לכל המשתתפים' : 'Please select a private key for all participants');
          return;
        }
      }

      this.loading = true;
      this.error = null;
      this.result = null;

      try {
        const res = await fetch('/api/aggregated/sign', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            PRIME_Q: this.q,
            A: this.a,
            B: this.b,
            Message: this.message,
            Participants: this.participants.map(p => ({ Name: p.name, PrivateKey: p.key })),
            Language: this.lang,
            IncludeDetailedReport: true
          })
        });
        const data = await res.json();
        if (!data.success) throw new Error(data.errorMessage || (isHe ? 'שגיאה לא ידועה' : 'Unknown error'));

        this.result = data;

        this.$nextTick(() => {
          this.$refs.resultsSection?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
      } catch (e) {
        this.error = e.message;
      } finally {
        this.loading = false;
      }
    },

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

    init() {
      this.$nextTick(() => this.renderKaTeX());
    }
  };
}
