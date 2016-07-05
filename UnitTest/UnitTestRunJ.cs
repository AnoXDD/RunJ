﻿using System;
using RunJ;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
    [TestClass]
    public class UnitTestRunJ {
        [TestMethod]
        public void TestNothing() {
            Assert.AreEqual(1 + 2, 3);
        }

        #region TestReplaceRegexGroups
        [TestMethod]
        public void TestReplaceRegexGroups_InsufficentArguments() {
            // Test command with insufficient arguments
            Assert.AreEqual("23-3,4",
                MainWindow.ReplaceRegexGroups("23-3,4", new string[] { "--{0}+{1}", "google{1}{0}" }));
            Assert.AreEqual("mail",
                MainWindow.ReplaceRegexGroups("mail", new string[] { "cal", "google{1}{0}" }));
        }

        [TestMethod]
        public void TestReplaceRegexGroups_ExcessiveSymbols() {
            // Test command with excessive symbols
            Assert.AreEqual("c 1 2 3 4 5",
                MainWindow.ReplaceRegexGroups("c 1 2 3 4 5", new string[] { "c {0} {1} {2} {3} {4} {5}", "hey{5}{4}{3}{2}{1}{0}" }));
            Assert.AreEqual("hey{5}54321",
                MainWindow.ReplaceRegexGroups("c 1 2 3 4 5 {5}",
                    new string[] { "c {0} {1} {2} {3} {4} {5}", "hey{5}{4}{3}{2}{1}{0}" }));
        }

        [TestMethod]
        public void TestReplaceRegexGroups_FiveSymbols() {
            // Test command with five symbols
            Assert.AreEqual("hey54321",
                MainWindow.ReplaceRegexGroups("c 1 2 3 4 5", new string[] { "c {0} {1} {2} {3} {4}", "hey{4}{3}{2}{1}{0}" }));
        }

        [TestMethod]
        public void TestReplaceRegexGroups_TwoSymbols() {
            // Test command with two symbols
            Assert.AreEqual("google323",
                MainWindow.ReplaceRegexGroups("--23-3", new string[] { "--{0}-{1}", "google{1}{0}" }));
        }

        [TestMethod]
        public void TestReplaceRegexGroups_RegexSymbols() {
            // Test command with regex symbols
            Assert.AreEqual("google[23]",
                MainWindow.ReplaceRegexGroups("?23", new string[] { "?{0}", "google[{0}]" }));
            Assert.AreEqual("google[23??]",
                MainWindow.ReplaceRegexGroups("?23??", new string[] { "?{0}", "google[{0}]" }));
            Assert.AreEqual("google[23]",
                MainWindow.ReplaceRegexGroups("?23??", new string[] { "?{0}??", "google[{0}]" }));
            Assert.AreEqual("google[3223]",
                MainWindow.ReplaceRegexGroups("?23??32?", new string[] { "?{0}??{1}?", "google[{1}{0}]" }));
        }

        [TestMethod]
        public void TestReplaceRegexGroupsNormal() {
            // Normal test
            Assert.AreEqual("google?[]23",
                MainWindow.ReplaceRegexGroups("--23", new string[] { "--{0}", "google?[]{0}" }));
        }
        #endregion 


    }
}