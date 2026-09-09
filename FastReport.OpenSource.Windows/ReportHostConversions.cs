using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace FastReport.Windows;

/// <summary>Explicit conversions at the native host boundary. No UI is initialized.</summary>
public static class ReportHostConversions
{
    public static Padding ToNative(Layout.Padding value) => new(value.Left, value.Top, value.Right, value.Bottom);
    public static Layout.Padding ToReport(Padding value) => new(value.Left, value.Top, value.Right, value.Bottom);
    public static AnchorStyles ToNative(Layout.AnchorStyles value) => (AnchorStyles)(int)value;
    public static Layout.AnchorStyles ToReport(AnchorStyles value) => (Layout.AnchorStyles)(int)value;
    public static DockStyle ToNative(Layout.DockStyle value) => (DockStyle)(int)value;
    public static Layout.DockStyle ToReport(DockStyle value) => (Layout.DockStyle)(int)value;
    public static PictureBoxSizeMode ToNative(Layout.ImageSizeMode value) => (PictureBoxSizeMode)(int)value;
    public static Layout.ImageSizeMode ToReport(PictureBoxSizeMode value) => (Layout.ImageSizeMode)(int)value;

    /// <summary>Returns a shared native cursor; the caller must not dispose it.</summary>
    public static Cursor GetCursor(string name) =>
        typeof(Cursors).GetProperty(name ?? "Default")?.GetValue(null) as Cursor
        ?? throw new ArgumentException("Unknown cursor name: " + name, nameof(name));

    /// <summary>Registers a real editor for a value type until the returned scope is disposed.
    /// Hosts own UI-thread dispatch and the editor implementation.</summary>
    public static IDisposable RegisterEditor(Type valueType, Type editorType)
    {
        if (!typeof(UITypeEditor).IsAssignableFrom(editorType))
            throw new ArgumentException("The editor must derive from native UITypeEditor.", nameof(editorType));
        var provider = new EditorProvider(TypeDescriptor.GetProvider(valueType), editorType);
        TypeDescriptor.AddProvider(provider, valueType);
        return new EditorRegistration(valueType, provider);
    }

    private sealed class EditorProvider(TypeDescriptionProvider parent, Type editorType) : TypeDescriptionProvider(parent)
    {
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) =>
            new EditorDescriptor(base.GetTypeDescriptor(objectType, instance), editorType);
    }

    private sealed class EditorDescriptor(ICustomTypeDescriptor parent, Type editorType) : CustomTypeDescriptor(parent)
    {
        public override object GetEditor(Type editorBaseType) => editorBaseType == typeof(UITypeEditor)
            ? Activator.CreateInstance(editorType) : base.GetEditor(editorBaseType);
    }

    private sealed class EditorRegistration(Type type, TypeDescriptionProvider provider) : IDisposable
    {
        private TypeDescriptionProvider registration = provider;
        public void Dispose()
        {
            if (registration == null) return;
            TypeDescriptor.RemoveProvider(registration, type);
            registration = null;
        }
    }
}
