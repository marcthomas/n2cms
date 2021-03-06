﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using N2.Collections;
using N2.Engine;
using N2.Edit;
using System.IO;
using N2.Web.UI.WebControls;

namespace N2.Web.Mvc.Html
{
	public static class HtmlExtensions
	{
		/// <summary>Creates a navigation using unordered list elements.</summary>
		public static N2.Web.Tree Tree(this HtmlHelper html,
			ContentItem startsFrom = null,
			ContentItem current = null,
			int takeLevels = 2,
			bool parallelRoot = true,
			bool appendCreatorNode = false,
			ItemFilter filter = null,
			object htmlAttributes = null)
		{
			if (startsFrom == null) startsFrom = html.StartPage();
			if (current == null) current = html.CurrentPage();
			if (filter == null) filter = new NavigationFilter(html.ViewContext.HttpContext.User, html.ContentEngine().SecurityManager);

			var builder = parallelRoot
				? (HierarchyBuilder)new ParallelRootHierarchyBuilder(startsFrom, takeLevels)
				: (HierarchyBuilder)new TreeHierarchyBuilder(startsFrom, takeLevels);

			if (appendCreatorNode && ControlPanelExtensions.GetControlPanelState(html).IsFlagSet(ControlPanelState.DragDrop))
				builder.GetChildren = (i) => i.Children.FindNavigatablePages().Where(filter).AppendCreatorNode(html.ContentEngine(), i);
			else
				builder.GetChildren = (i) => i.Children.FindNavigatablePages().Where(filter);

			var tree = N2.Web.Tree.Using(builder);
			if (htmlAttributes != null)
				tree.Tag(ApplyToRootUl(htmlAttributes));

			ClassifyAnchors(startsFrom, current, parallelRoot, tree);

			return tree;
		}

		private static void ClassifyAnchors(ContentItem startsFrom, ContentItem current, bool parallelRoot, Web.Tree tree)
		{
			var ancestors = N2.Find.ListParents(current, startsFrom, true);
			if (parallelRoot && ancestors.Contains(startsFrom))
				ancestors.Remove(startsFrom);

			tree.LinkWriter((n, w) => n.Current.Link().Class(n.Current == current ? "current" : ancestors.Contains(n.Current) ? "trail" : "").WriteTo(w));
		}

		private static Action<HierarchyNode<ContentItem>, TagBuilder> ApplyToRootUl(object htmlAttributes)
		{
			return (n, t) =>
			{
				if (t.TagName != "ul")
					return;
				if (n.Parent != null && n.Parent.Current != null)
					return;

				t.MergeAttributes(htmlAttributes);
			};
		}

		public static N2.Web.ILinkBuilder Link(this HtmlHelper html, ContentItem item)
		{
			return N2.Web.Link.To(item);
		}

		public static N2.Web.ILinkBuilder Link(this ContentItem item)
		{
			return N2.Web.Link.To(item);
		}

		/// <summary>Generates an id that is unique within a content item.</summary>
		/// <param name="html"></param>
		/// <param name="prefix"></param>
		/// <returns></returns>
		public static string UniqueID(this HtmlHelper html, string prefix = null)
		{
			if (string.IsNullOrEmpty(prefix))
			{
				var number = html.ViewContext.RequestContext.HttpContext.Items["SequentialUniqueID"];
				number = number == null ? 0 : 1 + (int)number;
				html.ViewContext.RequestContext.HttpContext.Items["SequentialUniqueID"] = number;

				return "id" + html.CurrentItem().ID + "_" + number;
			}

			return prefix + html.CurrentItem().ID;
		}

		/// <summary>Begins an editable wrapper that can be used to edit a single property in a view.</summary>
		/// <param name="html"></param>
		/// <param name="item"></param>
		/// <param name="displayableName"></param>
		/// <returns>A disposable object that must be called to close the editable wrapper element.</returns>
		public static IDisposable BeginEditableWrapper(this HtmlHelper html, ContentItem item, string displayableName)
		{
			return WebExtensions.GetEditableWrapper(item, true, displayableName, html.ContentEngine().Definitions.GetDefinition(item).Displayables[displayableName], html.ViewContext.Writer);
		}
	}
}
