<a name="samples"></a> <a name="samples-label"></a>

# Samples

[Documentation index](README.md)

*Graphic examples of loving a ListView*

These are examples of what can be done with an `ObjectListView`.

<table>
<colgroup>
<col style="width: 50%" />
<col style="width: 50%" />
</colgroup>
<tbody>
<tr>
<td><p><a href="#rearrangeable-treelistview">Rearrangeable TreeListView</a></p>
<p>Shows how to drag and drop branches on a <code>TreeListView</code>.</p></td>
<td><img src="images/dragdrop-tlv-small.png" alt="dragdrop tlv small" /></td>
</tr>
<tr>
<td><p><a href="#foobar-lookalike">Foobar Lookalike</a></p>
<p>Shows how to creatively use decorations, and hot item styles.</p></td>
<td><img src="images/foobar-lookalike-small.png" alt="foobar lookalike small" /></td>
</tr>
<tr>
<td><p><a href="#task-list">Task List</a></p>
<p>Shows a more sophisticated renderer as well as demonstrating how to combine a decoration with a renderer.</p></td>
<td><img src="images/task-list-small.png" alt="task list small" /></td>
</tr>
</tbody>
</table>

<a name="rearrangeable-treelistview"></a>

## Rearrangeable TreeListView

`TreeListViews` are cool, but it seems that they are tricky. One task that seems to cause problems for some people is how to make the tree rearrangeable -- that is, how can I let the users drag the branches around.

[This sample](http://sourceforge.net/projects/objectlistview/files/objectlistview/TreeListViewDragDrop.7z) shows how to do exactly that, and [this blog](blog4.md#blog-rearrangingtreelistview) describes the whole process.

![dragdrop tlv](images/dragdrop-tlv.png) <a name="foobar-lookalike"></a>

## Foobar Lookalike

Those who dwell in the house of cool know that black goes with everything.

![foobar lookalike](images/foobar-lookalike.png)

A normal ListView cannot display things that exceed the bounds of their cells. But `ObjectListView` has decorations and overlays so it is much more flexible.

The dark background is a simple `BackColor` setting. The different colored text is handled throught a `FormatCell` event. The hot row highlighting is handled through normal `HotItemStyle` mechanism. The only interesting bit is that it uses the `LeftColumn` property to limit the cells that are highlighted as hot.

The clever bit is getting the album artwork into the control. This is done through two types of decorations. In the first row in an album, cell 0 is given an `ImageDecoration` which shows a thumbnail of the artwork. This works, but the artwork isn't always correctly redrawn: it's only redrawn properly when cell 0 is visible.

To get around this problem, we install proxy decorations on cell 0 of all the other rows in the album. These proxies simply make the artwork decoration draw itself. In this way, the artwork is redrawn even when cell 0 of the first row is not visible.

[Download from here](http://sourceforge.net/projects/objectlistview/files/objectlistview/FoobarLookalike.7z)

<a name="task-list"></a>

## Task List

The Foobar example is cool, but this task list is useful. Displaying this kind of icon-title-description combination is a common task in many UI situations.

![task list](images/task-list.png)

This sample shows a custom `Decoration` that draws an icon, title and multiline description.

`ObjectListView` comes with `DescribedTaskRenderer` which is a `Renderer` that does the same task.

[Download sample project from here](http://sourceforge.net/projects/objectlistview/files/objectlistview/VistaSelection.7z)
