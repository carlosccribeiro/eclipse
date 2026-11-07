using System;
using NUnit.Framework;

namespace Eclipse.Core.DataTables.Tests
{
    public class CsvTableTests
    {
        private enum Kind { Fire, Ice }

        [Test]
        public void Parses_header_and_rows_with_lf_and_crlf()
        {
            foreach (string nl in new[] { "\n", "\r\n" })
            {
                var t = CsvTable.Parse("id,a" + nl + "x,1" + nl + "y,2" + nl);
                Assert.AreEqual(new[] { "id", "a" }, t.Columns);
                Assert.AreEqual(2, t.Rows.Count);
                Assert.AreEqual("y", t.Rows[1].GetString("id"));
                Assert.AreEqual(2, t.Rows[1].GetInt("a"));
                Assert.IsTrue(t.HasColumn("a"));
                Assert.IsFalse(t.HasColumn("zz"));
                Assert.AreEqual(1, t.IndexOf("a"));
            }
        }

        [Test]
        public void Ignores_bom_and_blank_lines_and_tracks_lines()
        {
            var t = CsvTable.Parse("﻿id,a\n\nx,1\n\r\n\ny,2");
            Assert.AreEqual(2, t.Rows.Count);
            Assert.AreEqual(3, t.Rows[0].Line);
            Assert.AreEqual(6, t.Rows[1].Line);
        }

        [Test]
        public void Quoted_fields_support_comma_newline_and_escaped_quotes()
        {
            var t = CsvTable.Parse("id,txt\nx,\"a,b\nc \"\"q\"\"\"\ny,\"\"\n");
            Assert.AreEqual("a,b\nc \"q\"", t.Rows[0].GetString("txt"));
            Assert.IsTrue(t.Rows[1].IsEmpty("txt"));
            Assert.AreEqual(4, t.Rows[1].Line);
        }

        [Test]
        public void Empty_cells_and_short_rows()
        {
            var t = CsvTable.Parse("id,a,b\nx,,\ny");
            Assert.IsTrue(t.Rows[0].IsEmpty("a"));
            Assert.AreEqual(7, t.Rows[0].GetInt("a", 7));
            Assert.IsTrue(t.Rows[1].IsEmpty("b"));
        }

        [Test]
        public void Header_errors()
        {
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse(""));
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("\n\n"));
            var ex = Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id,a,a\n"));
            StringAssert.Contains("duplicate", ex.Message);
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id,,a\n"));
        }

        [Test]
        public void Structure_errors()
        {
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id\nx,1,2"));
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id\n\"abc"));
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id\na\"b"));
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id\n\"a\"b"));
        }

        [Test]
        public void Typed_getters_use_invariant_culture()
        {
            var t = CsvTable.Parse("id,f,i,b,e\nx,1.5,-3,TRUE,Ice\n");
            var r = t.Rows[0];
            Assert.AreEqual(1.5f, r.GetFloat("f"));
            Assert.AreEqual(-3, r.GetInt("i"));
            Assert.IsTrue(r.GetBool("b"));
            Assert.AreEqual(Kind.Ice, r.GetEnum<Kind>("e"));
        }

        [Test]
        public void Optional_getters_return_default_when_empty()
        {
            var r = CsvTable.Parse("id,a\nx,\n").Rows[0];
            Assert.AreEqual("d", r.GetString("a", "d"));
            Assert.AreEqual(2.5f, r.GetFloat("a", 2.5f));
            Assert.IsTrue(r.GetBool("a", true));
            Assert.AreEqual(Kind.Fire, r.GetEnum("a", Kind.Fire));
            Assert.Throws<CsvFormatException>(() => r.GetInt("a"));
        }

        [Test]
        public void Lists_split_and_trim()
        {
            var r = CsvTable.Parse("id,l\nx,a | b|c\ny,\n").Rows[0];
            Assert.AreEqual(new[] { "a", "b", "c" }, r.GetList("l"));
            Assert.AreEqual(0, CsvTable.Parse("id,l\ny,\n").Rows[0].GetList("l").Count);
        }

        [Test]
        public void Error_messages_include_line_column_and_value()
        {
            var t = CsvTable.Parse("id,n\nx,1\ny,abc\n");
            var ex = Assert.Throws<CsvFormatException>(() => t.Rows[1].GetFloat("n"));
            StringAssert.Contains("line 3", ex.Message);
            StringAssert.Contains("'n'", ex.Message);
            StringAssert.Contains("abc", ex.Message);
            Assert.AreEqual(3, ex.Line);

            ex = Assert.Throws<CsvFormatException>(() => t.Rows[0].GetString("nope"));
            StringAssert.Contains("nope", ex.Message);
            StringAssert.Contains("line 2", ex.Message);

            var e2 = CsvTable.Parse("id,e\nx,Wind\n");
            ex = Assert.Throws<CsvFormatException>(() => e2.Rows[0].GetEnum<Kind>("e"));
            StringAssert.Contains("Wind", ex.Message);
            StringAssert.Contains("Fire, Ice", ex.Message);

            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id,b\nx,yes\n").Rows[0].GetBool("b"));
            Assert.Throws<CsvFormatException>(() => CsvTable.Parse("id,e\nx,1\n").Rows[0].GetEnum<Kind>("e"));
        }

        [Test]
        public void Null_text_throws()
        {
            Assert.Throws<ArgumentNullException>(() => CsvTable.Parse(null));
        }
    }
}
