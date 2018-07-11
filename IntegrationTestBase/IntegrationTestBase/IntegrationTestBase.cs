using System.IO;
using System;

namespace IntegrationTestBase
{
   
        /// <summary>
        /// Provides basic path information for integration tests, to provide a common location for artifacts and temp files.
        /// </summary>
        /// <remarks>
        /// Any file path resolution should be done through this base class.
        /// </remarks>
        public class IntegrationTestBase
        {
            #region static functionality.
            
                private static readonly string BasePath;
                private static readonly string ArtifactsBasePath;

                static IntegrationTestBase()
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    BasePath = Path.Combine(currentDirectory, "_TempData");
                    ArtifactsBasePath = Path.Combine(currentDirectory, "_Artifacts");

                    CreateTempPath();
                }

                /// <summary>
                /// Returns the path to the base temp folder
                /// </summary>
                /// <returns></returns>
                public static string GetTestTempBasePath()
                {
                    return BasePath;
                }

                //Removes and creates the test run temp folder.  Should be called only when test suite first starts.
                private static void CreateTempPath()
                {
                    //Remove temp / artifacts folders if they exist.
                    if (Directory.Exists(BasePath))                
                        Directory.Delete(BasePath, true);                

                    if (Directory.Exists(ArtifactsBasePath))                
                        Directory.Delete(ArtifactsBasePath, true);                

                    //Ensure temp folder and artifact folder exist.
                    Directory.CreateDirectory(BasePath);
                    Directory.CreateDirectory(ArtifactsBasePath);
                }

            #endregion

            private string _uniqueName;
            private string _artifactFolder;

            public IntegrationTestBase()
            {
                string testName;
                string testFullName;

                //Try get from nunit.
                var curTestContext = NUnit.Framework.TestContext.CurrentContext;
                if (curTestContext != null)
                {
                    testName = curTestContext.Test.Name;
                    testFullName = curTestContext.Test.FullName;
                }
                else
                {
                    testName = Path.GetRandomFileName();
                    testFullName = "(Test Context Not Available)";
                }

                //Get the artifact folder.
                string uniqueTestName = testName;
                string artifactFolder = Path.Combine(ArtifactsBasePath, uniqueTestName);
                int curNum = 2;
                while (Directory.Exists(artifactFolder))
                {
                    uniqueTestName = testName + "_" + curNum++;
                    artifactFolder = Path.Combine(ArtifactsBasePath, uniqueTestName);
                }
                _uniqueName = uniqueTestName;
                _artifactFolder = artifactFolder;
                Directory.CreateDirectory(artifactFolder);

                //Write out the full name of the test to it (for identification if required)
                var contents = $"Artifact for test: {testFullName}{Environment.NewLine}Unique Test Id: {_uniqueName}";

                File.WriteAllText(Path.Combine(_artifactFolder, "TestInfo.txt"), contents);
            }

            /// <summary>
            /// Gets the folder to store artifacts for this test.  This defaults to TestArtifacts/{unique name of test}.
            /// </summary>
            /// <returns></returns>
            protected virtual string GetArtifactFolder() => _artifactFolder;

            /// <summary>
            /// Returns a unique temp file name that can be used for this test.  Use this in tests to store temp data
            /// in a uniform location.  This defaults to storing data under TestTempData/Tests/{unique name of test}.
            /// </summary>
            /// <returns></returns>
            protected virtual string GetTempFileName() => Path.Combine(BasePath, "Tests", _uniqueName, Path.GetTempFileName());

            public virtual string CurrentTestUniqueName => _uniqueName;
        }
    }
