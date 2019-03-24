﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    public abstract class Dropdown<T> : FillFlowContainer, IHasCurrentValue<T>
    {
        protected internal DropdownHeader Header;
        protected internal DropdownMenu Menu;

        /// <summary>
        /// Creates the header part of the control.
        /// </summary>
        protected abstract DropdownHeader CreateHeader();

        /// <summary>
        /// A mapping from menu items to their values.
        /// </summary>
        private readonly Dictionary<T, DropdownMenuItem<T>> itemMap = new Dictionary<T, DropdownMenuItem<T>>();

        protected IEnumerable<DropdownMenuItem<T>> MenuItems => itemMap.Values;

        /// <summary>
        /// Enumerate all values in the dropdown.
        /// </summary>
        public IEnumerable<T> Items
        {
            get => MenuItems.Select(i => i.Value);
            set
            {
                if (usingItemSource)
                    throw new InvalidOperationException($"Cannot manually set {nameof(Items)} when an {nameof(ItemSource)} is bound.");
                setItems(value);
            }
        }

        private void setItems(IEnumerable<T> items)
        {
            clearItems();
            if (items == null)
                return;

            foreach (var entry in items)
                addDropdownItem(GenerateItemText(entry), entry);

            if (Current.Value == null || !itemMap.Keys.Contains(Current.Value))
                Current.Value = itemMap.Keys.FirstOrDefault();
            else
                Current.TriggerChange();
        }

        private readonly IBindableList<T> itemSource = new BindableList<T>();
        private bool usingItemSource;

        /// <summary>
        /// Allows the developer to assign an <see cref="IBindableList{T}"/> as the source
        /// of items for this dropdown.
        /// </summary>
        public IBindableList<T> ItemSource
        {
            get => itemSource;
            set
            {
                itemSource.UnbindBindings();
                usingItemSource = value != null;

                if (value == null)
                    setItems(null);
                else
                    itemSource.BindTo(value);
            }
        }

        /// <summary>
        /// Add a menu item directly while automatically generating a label.
        /// </summary>
        /// <param name="value">Value selected by the menu item.</param>
        public void AddDropdownItem(T value) => AddDropdownItem(GenerateItemText(value), value);

        /// <summary>
        /// Add a menu item directly.
        /// </summary>
        /// <param name="text">Text to display on the menu item.</param>
        /// <param name="value">Value selected by the menu item.</param>
        protected void AddDropdownItem(string text, T value)
        {
            if (usingItemSource)
                throw new InvalidOperationException($"Cannot manually add dropdown items when an {nameof(ItemSource)} is bound.");
            addDropdownItem(text, value);
        }

        private void addDropdownItem(string text, T value)
        {
            if (itemMap.ContainsKey(value))
                throw new ArgumentException($"The item {value} already exists in this {nameof(Dropdown<T>)}.");

            var newItem = new DropdownMenuItem<T>(text, value, () =>
            {
                if (!Current.Disabled)
                    Current.Value = value;

                Menu.State = MenuState.Closed;
            });

            Menu.Add(newItem);
            itemMap[value] = newItem;
        }

        /// <summary>
        /// Remove a menu item directly.
        /// </summary>
        /// <param name="value">Value of the menu item to be removed.</param>
        public bool RemoveDropdownItem(T value)
        {
            if (usingItemSource)
                throw new InvalidOperationException($"Cannot manually remove items when an {nameof(ItemSource)} is bound.");
            return removeDropdownItem(value);
        }

        private bool removeDropdownItem(T value)
        {
            if (value == null)
                return false;

            if (!itemMap.TryGetValue(value, out var item))
                return false;

            Menu.Remove(item);
            itemMap.Remove(value);

            return true;
        }

        protected virtual string GenerateItemText(T item)
        {
            switch (item)
            {
                case MenuItem i:
                    return i.Text.Value;
                case IHasText t:
                    return t.Text;
                case Enum e:
                    return e.GetDescription();
                default:
                    return item?.ToString() ?? "null";
            }
        }

        private readonly Bindable<T> current = new Bindable<T>();

        public Bindable<T> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        private DropdownMenuItem<T> selectedItem;

        protected DropdownMenuItem<T> SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                if (value != null)
                    Current.Value = value.Value;
            }
        }

        protected Dropdown()
        {
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;

            Children = new Drawable[]
            {
                Header = CreateHeader(),
                Menu = CreateMenu()
            };

            Menu.RelativeSizeAxes = Axes.X;

            Header.Action = Menu.Toggle;
            Header.ChangeSelection += selectionKeyPressed;
            Menu.ChangePreselection += preselectionKeyPressed;

            Current.ValueChanged += selectionChanged;

            ItemSource.ItemsAdded += _ => setItems(ItemSource);
            ItemSource.ItemsRemoved += _ => setItems(ItemSource);
        }

        private void preselectionKeyPressed(int selectedIndex)
        {
            SelectedItem = MenuItems.ElementAt(selectedIndex);
            Menu.State = MenuState.Closed;
        }

        private void selectionKeyPressed(DropdownHeader.SelectionChange change)
        {
            var dropdownMenuItems = MenuItems.ToList();
            if (!dropdownMenuItems.Any()) return;

            var selectedIndex = dropdownMenuItems.IndexOf(SelectedItem);

            switch (change)
            {
                case DropdownHeader.SelectionChange.Previous:
                    selectedIndex = MathHelper.Clamp(selectedIndex - 1, 0, dropdownMenuItems.Count - 1);
                    break;
                case DropdownHeader.SelectionChange.Next:
                    selectedIndex = MathHelper.Clamp(selectedIndex + 1, 0, dropdownMenuItems.Count - 1);
                    break;
                case DropdownHeader.SelectionChange.First:
                    selectedIndex = 0;
                    break;
                case DropdownHeader.SelectionChange.Last:
                    selectedIndex = dropdownMenuItems.Count - 1;
                    break;
                default:
                    throw new ArgumentException("Unexpected selection change type.", nameof(change));
            }

            SelectedItem = dropdownMenuItems[selectedIndex];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text.Value;
        }

        private void selectionChanged(ValueChangedEvent<T> args)
        {
            // refresh if SelectedItem and SelectedValue mismatched
            // null is not a valid value for Dictionary, so neither here
            if ((SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, args.NewValue))
                && args.NewValue != null)
            {
                if (!itemMap.TryGetValue(args.NewValue, out selectedItem))
                {
                    selectedItem = new DropdownMenuItem<T>(GenerateItemText(args.NewValue), args.NewValue);
                }
            }

            Menu.SelectItem(selectedItem);
            Header.Label = selectedItem.Text.Value;
        }

        /// <summary>
        /// Clear all the menu items.
        /// </summary>
        public void ClearItems()
        {
            if (usingItemSource)
                throw new InvalidOperationException($"Cannot manually clear items when an {nameof(ItemSource)} is bound.");
            clearItems();
        }

        private void clearItems()
        {
            itemMap.Clear();
            Menu.Clear();
        }

        /// <summary>
        /// Hide the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to hide.</param>
        internal void HideItem(T val)
        {
            if (itemMap.TryGetValue(val, out DropdownMenuItem<T> item))
            {
                Menu.HideItem(item);
                updateHeaderVisibility();
            }
        }

        /// <summary>
        /// Show the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to show.</param>
        internal void ShowItem(T val)
        {
            if (itemMap.TryGetValue(val, out DropdownMenuItem<T> item))
            {
                Menu.ShowItem(item);
                updateHeaderVisibility();
            }
        }

        private void updateHeaderVisibility() => Header.Alpha = Menu.AnyPresent ? 1 : 0;

        protected override bool OnHover(HoverEvent e) => true;

        /// <summary>
        /// Creates the menu body.
        /// </summary>
        protected virtual DropdownMenu CreateMenu() => new DropdownMenu();

        #region DropdownMenu

        public class DropdownMenu : Menu, IKeyBindingHandler<PlatformAction>
        {
            public DropdownMenu()
                : base(Direction.Vertical)
            {
                StateChanged += clearPreselection;
            }

            private void clearPreselection(MenuState obj)
            {
                if (obj == MenuState.Closed)
                    PreselectItem(null);
            }

            protected internal IEnumerable<DrawableDropdownMenuItem> DrawableMenuItems => Children.OfType<DrawableDropdownMenuItem>();
            protected internal IEnumerable<DrawableDropdownMenuItem> VisibleMenuItems => DrawableMenuItems.Where(item => !item.IsMaskedAway);

            public DrawableDropdownMenuItem PreselectedItem => Children.OfType<DrawableDropdownMenuItem>().FirstOrDefault(c => c.IsPreSelected);

            public event Action<int> ChangePreselection;

            /// <summary>
            /// Selects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            public void SelectItem(DropdownMenuItem<T> item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    c.IsSelected = c.Item == item;
                    if (c.IsSelected)
                        ContentContainer.ScrollIntoView(c);
                });
            }

            /// <summary>
            /// Preselects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            public void PreselectItem(DropdownMenuItem<T> item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    c.IsPreSelected = c.Item == item;
                    if (c.IsPreSelected)
                        ContentContainer.ScrollIntoView(c);
                });
            }

            /// <summary>
            /// Shows an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to show.</param>
            public void HideItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => c.Item == item)?.Hide();

            /// <summary>
            /// Hides an item from this <see cref="DropdownMenu"/>
            /// </summary>
            /// <param name="item"></param>
            public void ShowItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => c.Item == item)?.Show();

            /// <summary>
            /// Whether any items part of this <see cref="DropdownMenu"/> are present.
            /// </summary>
            public bool AnyPresent => Children.Any(c => c.IsPresent);

            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableDropdownMenuItem(item);

            #region DrawableDropdownMenuItem

            // must be public due to mono bug(?) https://github.com/ppy/osu/issues/1204
            public class DrawableDropdownMenuItem : DrawableMenuItem
            {
                public DrawableDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                }

                private bool selected;

                public bool IsSelected
                {
                    get => !Item.Action.Disabled && selected;
                    set
                    {
                        if (selected == value)
                            return;
                        selected = value;

                        OnSelectChange();
                    }
                }

                private bool preSelected;

                public bool IsPreSelected
                {
                    get => preSelected;
                    set
                    {
                        if (preSelected == value)
                            return;
                        preSelected = value;

                        OnSelectChange();
                    }
                }

                private Color4 backgroundColourSelected = Color4.SlateGray;

                public Color4 BackgroundColourSelected
                {
                    get => backgroundColourSelected;
                    set
                    {
                        backgroundColourSelected = value;
                        UpdateBackgroundColour();
                    }
                }

                private Color4 foregroundColourSelected = Color4.White;

                public Color4 ForegroundColourSelected
                {
                    get => foregroundColourSelected;
                    set
                    {
                        foregroundColourSelected = value;
                        UpdateForegroundColour();
                    }
                }

                protected virtual void OnSelectChange()
                {
                    if (!IsLoaded)
                        return;

                    UpdateBackgroundColour();
                    UpdateForegroundColour();
                }

                protected override void UpdateBackgroundColour()
                {
                    Background.FadeColour(IsPreSelected ? BackgroundColourHover : IsSelected ? BackgroundColourSelected : BackgroundColour);
                }

                protected override void UpdateForegroundColour()
                {
                    Foreground.FadeColour(IsPreSelected ? ForegroundColourHover : IsSelected ? ForegroundColourSelected : ForegroundColour);
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    Background.Colour = IsSelected ? BackgroundColourSelected : BackgroundColour;
                    Foreground.Colour = IsSelected ? ForegroundColourSelected : ForegroundColour;
                }

                private DropdownMenu menu;

                /// <summary>
                /// Retrieve the first parent in the tree which is <see cref="DropdownMenu"/>.
                /// As this is performing an upward tree traversal, avoid calling every frame.
                /// </summary>
                /// <returns>The first parent <see cref="DropdownMenu"/>.</returns>
                private DropdownMenu getMenu()
                {
                    Drawable search = Parent;
                    while (search != null)
                    {
                        if (search is DropdownMenu test) return test;

                        search = search.Parent;
                    }

                    return null;
                }

                protected override bool OnHover(HoverEvent e)
                {
                    if (menu == null)
                        menu = getMenu();

                    menu.PreselectItem(Item as DropdownMenuItem<T>);
                    return base.OnHover(e);
                }
            }

            #endregion

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case KeyDownEvent keyDown:
                        var drawableMenuItemsList = DrawableMenuItems.ToList();
                        var preselectedItem = drawableMenuItemsList.FirstOrDefault(i => i.IsPreSelected) ?? drawableMenuItemsList.First(i => i.IsSelected);
                        var preselectedIndex = drawableMenuItemsList.IndexOf(preselectedItem);

                        int clampIndex(int index) => MathHelper.Clamp(index, 0, drawableMenuItemsList.Count - 1);

                        switch (keyDown.Key)
                        {
                            case Key.Up:
                                PreselectItem((DropdownMenuItem<T>)Items[clampIndex(preselectedIndex - 1)]);
                                return true;
                            case Key.Down:
                                PreselectItem((DropdownMenuItem<T>)Items[clampIndex(preselectedIndex + 1)]);
                                return true;
                            case Key.PageUp:
                                var firstVisibleItem = VisibleMenuItems.First();
                                preselectedIndex = preselectedItem == firstVisibleItem
                                    ? clampIndex(preselectedIndex - VisibleMenuItems.Count())
                                    : drawableMenuItemsList.IndexOf(firstVisibleItem);
                                PreselectItem((DropdownMenuItem<T>)Items[preselectedIndex]);
                                return true;
                            case Key.PageDown:
                                var lastVisibleItem = VisibleMenuItems.Last();
                                preselectedIndex = preselectedItem == lastVisibleItem
                                    ? clampIndex(preselectedIndex + VisibleMenuItems.Count())
                                    : drawableMenuItemsList.IndexOf(lastVisibleItem);
                                PreselectItem((DropdownMenuItem<T>)Items[preselectedIndex]);
                                return true;
                            case Key.Enter:
                                ChangePreselection?.Invoke(preselectedIndex);
                                return true;
                            default:
                                return base.Handle(e);
                        }
                    default:
                        return base.Handle(e);
                }
            }

            public bool OnPressed(PlatformAction action)
            {
                switch (action.ActionType)
                {
                    case PlatformActionType.ListStart:
                        PreselectItem((DropdownMenuItem<T>)Items.First());
                        return true;
                    case PlatformActionType.ListEnd:
                        PreselectItem((DropdownMenuItem<T>)Items.Last());
                        return true;
                    default:
                        return false;
                }
            }

            public bool OnReleased(PlatformAction action) => false;
        }

        #endregion
    }
}
