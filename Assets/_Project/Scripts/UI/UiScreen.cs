using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Base for every code-built screen. Screens are built once and shown / hidden by the GameFlow.</summary>
    public abstract class UiScreen
    {
        protected GameFlow flow;
        public RectTransform Root { get; private set; }
        public bool Visible { get; private set; }

        protected GameDatabase Db => flow.Db;
        protected MetaService Meta => flow.Meta;
        protected Controls Input => flow.Controls;

        public void Build(Transform canvas, GameFlow flow, string name)
        {
            this.flow = flow;
            Root = Ui.Node(canvas, name).Stretch();
            BuildContent();
            Root.gameObject.SetActive(false);
        }

        protected abstract void BuildContent();

        public virtual void Show()
        {
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            Visible = true;
            Refresh();
            var d = DefaultSelection;
            if (d != null) Ui.Select(d);
        }

        public virtual void Hide()
        {
            Visible = false;
            Root.gameObject.SetActive(false);
        }

        /// <summary>Rebuild dynamic content from current state.</summary>
        public virtual void Refresh() { }

        /// <summary>Per-frame update while visible (unscaled time).</summary>
        public virtual void Tick() { }

        /// <summary>B / Esc pressed.</summary>
        public virtual void OnBack() { }

        public virtual GameObject DefaultSelection => null;

        /// <summary>Re-focus something when the controller has nothing selected.</summary>
        public void EnsureSelection()
        {
            var s = Ui.Selected;
            if (s != null && s.activeInHierarchy) return;
            var d = DefaultSelection;
            if (d != null && d.activeInHierarchy) Ui.Select(d);
        }
    }
}
