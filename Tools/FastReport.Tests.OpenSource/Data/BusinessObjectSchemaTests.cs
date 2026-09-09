using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FastReport.Data;
using Xunit;

namespace FastReport.Tests.OpenSource.Data
{
    public class BusinessObjectSchemaTests
    {
        public class Row
        {
            public string Name { get; set; }
            public List<Row> Children { get; set; }
        }

        [Fact]
        public void EmptyArraysAndDeclaredNestedListsHaveSchema()
        {
            using var report = new Report();
            report.RegisterData(Array.Empty<Row>(), "Rows", 3);
            var source = report.GetDataSource("Rows");
            Assert.NotNull(source.Columns.FindByName("Name"));
            Assert.NotNull(source.Columns.FindByName("Children").Columns.FindByName("Name"));
            report.RegisterData(new List<Row>(), "List");
            Assert.NotNull(report.GetDataSource("List").Columns.FindByName("Name"));
        }

        [Fact]
        public void NonGenericListsUseBoundedNonNullSamples()
        {
            using var report = new Report();
            report.RegisterData(new ArrayList { null, new Row { Name = "A" } }, "Rows");
            Assert.NotNull(report.GetDataSource("Rows").Columns.FindByName("Name"));
        }

        [Fact]
        public void TypedListSchemaTakesPrecedenceAndListSourceLoadsRows()
        {
            using var report = new Report();
            report.Dictionary.RegisterData(new ListSource(), "Rows", true);
            var source = report.GetDataSource("Rows");
            Assert.NotNull(source.Columns.FindByName("Name"));
            Assert.Null(source.Columns.FindByName("Children"));
            source.Init();
            Assert.Equal(1, source.RowCount);
            Assert.Equal("A", source["Name"]);
        }

        [Fact]
        public void DiscoveryDoesNotAdvanceOneShotSequencesAndLoadingDisposesEnumerator()
        {
            var data = new OneShot();
            using var report = new Report();
            report.RegisterData(data, "Rows");
            Assert.Equal(0, data.Starts);
            var source = report.GetDataSource("Rows");
            source.Enabled = true;
            source.Init();
            Assert.Equal(2, source.RowCount);
            Assert.Equal("A", source["Name"]);
            source.Next();
            Assert.Equal("B", source["Name"]);
            Assert.True(data.Disposed);
        }

        [Fact]
        public void CustomDescriptorFactoryAndFilterHooksArePreserved()
        {
            var created = new DynamicRow();
            GetTypeInstanceEventHandler factory = (sender, args) =>
            {
                if (args.Type == typeof(DynamicRow)) args.Instance = created;
            };
            FilterPropertiesEventHandler filter = (sender, args) =>
            {
                if (sender is DynamicRow[] && args.Property.Name == "Children") args.Skip = true;
            };
            Utils.Config.ReportSettings.GetBusinessObjectTypeInstance += factory;
            Utils.Config.ReportSettings.FilterBusinessObjectProperties += filter;
            try
            {
                using var report = new Report();
                report.RegisterData(Array.Empty<DynamicRow>(), "Dynamic");
                Assert.NotNull(report.GetDataSource("Dynamic").Columns.FindByName("Name"));
                Assert.Null(report.GetDataSource("Dynamic").Columns.FindByName("Children"));
                Assert.True(created.Disposed);
                var supplied = new DynamicRow();
                report.RegisterData(new[] { supplied }, "Supplied");
                Assert.False(supplied.Disposed);
            }
            finally
            {
                Utils.Config.ReportSettings.GetBusinessObjectTypeInstance -= factory;
                Utils.Config.ReportSettings.FilterBusinessObjectProperties -= filter;
            }
        }

        public class DynamicRow : CustomTypeDescriptor, IDisposable
        {
            public bool Disposed;
            public override PropertyDescriptorCollection GetProperties() => TypeDescriptor.GetProperties(typeof(Row));
            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();
            public void Dispose() => Disposed = true;
        }

        private class TypedRows : ArrayList, ITypedList
        {
            public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] accessors) =>
                new PropertyDescriptorCollection(new[] { TypeDescriptor.GetProperties(typeof(Row))["Name"] });
            public string GetListName(PropertyDescriptor[] accessors) => "Rows";
        }

        private class ListSource : IListSource
        {
            public bool ContainsListCollection => false;
            public IList GetList() => new TypedRows { new Row { Name = "A" } };
        }

        private class OneShot : IEnumerable<Row>
        {
            public int Starts;
            public bool Disposed;
            public IEnumerator<Row> GetEnumerator()
            {
                if (++Starts > 1) throw new InvalidOperationException("Sequence restarted");
                return Rows().GetEnumerator();
            }
            private IEnumerable<Row> Rows()
            {
                try
                {
                    yield return new Row { Name = "A" };
                    yield return new Row { Name = "B" };
                }
                finally { Disposed = true; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
