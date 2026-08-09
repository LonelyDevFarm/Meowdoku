using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Core.UI
{
    public abstract class UIBaseWindow : MonoBehaviour
    {
        private readonly List<Action> _managedCleanup = new();
        private readonly List<Coroutine> _managedCoroutines = new();
        private bool _destroyLifecycleRan;

        public UiWindowState WindowState { get; private set; } =
            UiWindowState.Invalid;

        public bool IsShowing => WindowState == UiWindowState.Showing;
        public bool IsHidden => WindowState == UiWindowState.Hidden;

        internal void CreateLifecycle(UIManager manager)
        {
            if (WindowState != UiWindowState.Invalid) return;
            WindowState = UiWindowState.Creating;
            gameObject.SetActive(true);
            BindButtonGuards(manager);
            OnCreate();
            WindowState = UiWindowState.Hidden;
            gameObject.SetActive(false);
        }

        internal bool ShowLifecycle(
            IReadOnlyDictionary<string, object> parameters)
        {
            if (WindowState == UiWindowState.Showing)
            {
                OnShow(parameters);
                return false;
            }

            WindowState = UiWindowState.Showing;
            gameObject.SetActive(true);
            OnShow(parameters);
            return true;
        }

        internal void BeginClosingLifecycle()
        {
            if (WindowState == UiWindowState.Showing)
                WindowState = UiWindowState.Closing;
        }

        internal IEnumerator HideLifecycle()
        {
            IEnumerator hide = OnHide();
            if (hide != null) yield return hide;
            if (WindowState != UiWindowState.Closing) yield break;

            WindowState = UiWindowState.Hidden;
            ClearManagedLifetime();
            gameObject.SetActive(false);
        }

        internal void DestroyLifecycle()
        {
            if (_destroyLifecycleRan) return;
            _destroyLifecycleRan = true;
            WindowState = UiWindowState.Destroyed;
            OnDestroyWindow();
            ClearManagedLifetime();
        }

        protected virtual void OnCreate() { }

        protected virtual void OnShow(
            IReadOnlyDictionary<string, object> parameters) { }

        protected virtual IEnumerator OnHide()
        {
            yield break;
        }

        protected virtual void OnDestroyWindow() { }

        protected void ManageCleanup(Action cleanup)
        {
            if (cleanup != null) _managedCleanup.Add(cleanup);
        }

        protected Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            if (routine == null) return null;
            Coroutine coroutine = StartCoroutine(routine);
            _managedCoroutines.Add(coroutine);
            return coroutine;
        }

        private void BindButtonGuards(UIManager manager)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                UIButtonPressGuard guard =
                    button.GetComponent<UIButtonPressGuard>();
                if (guard == null)
                    guard = button.gameObject.AddComponent<UIButtonPressGuard>();
                guard.Bind(manager);
            }
        }

        private void ClearManagedLifetime()
        {
            foreach (Action cleanup in _managedCleanup)
                cleanup?.Invoke();
            _managedCleanup.Clear();

            foreach (Coroutine coroutine in _managedCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            _managedCoroutines.Clear();
        }

        private void OnDestroy()
        {
            DestroyLifecycle();
        }
    }
}
