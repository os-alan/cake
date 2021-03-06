﻿using System.Collections.Generic;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using Cake.Core.IO;
using Cake.Testing.Fakes;
using NSubstitute;

namespace Cake.Common.Tests.Fixtures.Tools
{
    internal sealed class MSBuildRunnerFixture
    {
        public IFileSystem FileSystem { get; set; }
        public ICakeEnvironment Environment { get; set; }
        public IProcess Process { get; set; }
        public IProcessRunner ProcessRunner { get; private set; }
        public IGlobber Globber { get; set; }

        public FilePath Solution { get; set; }
        public MSBuildSettings Settings { get; set; }

        public MSBuildRunnerFixture(IEnumerable<DirectoryPath> existingMSBuildPaths)
            : this(false, false, existingMSBuildPaths)
        {
        }

        public MSBuildRunnerFixture(bool is64BitOperativeSystem, bool msBuildFileExist)
            : this(is64BitOperativeSystem, msBuildFileExist, null)
        {
        }

        private MSBuildRunnerFixture(bool is64BitOperativeSystem, bool msBuildFileExist, IEnumerable<DirectoryPath> existingMSBuildPaths)
        {
            Process = Substitute.For<IProcess>();

            ProcessRunner = Substitute.For<IProcessRunner>();
            ProcessRunner.Start(Arg.Any<FilePath>(), Arg.Any<ProcessSettings>()).Returns(Process);

            Globber = Substitute.For<IGlobber>();

            Environment = Substitute.For<ICakeEnvironment>();
            Environment.Is64BitOperativeSystem().Returns(is64BitOperativeSystem);
            Environment.GetSpecialPath(SpecialPath.ProgramFilesX86).Returns("/Program86");
            Environment.GetSpecialPath(SpecialPath.Windows).Returns("/Windows");
            Environment.IsUnix().Returns(true);
            Environment.WorkingDirectory.Returns("/Working");

            Solution = new FilePath("./src/Solution.sln");
            Settings = new MSBuildSettings();
            Settings.ToolVersion = MSBuildToolVersion.VS2013;

            if (existingMSBuildPaths != null)
            {
                // Add all existing MSBuild tool paths.
                var fileSystem = new FakeFileSystem(Environment);
                FileSystem = fileSystem;
                foreach (var existingPath in existingMSBuildPaths)
                {
                    fileSystem.CreateDirectory(existingPath);
                    fileSystem.CreateFile(existingPath.GetFilePath("MSBuild.exe"));
                }
            }
            else
            {
                FileSystem = Substitute.For<IFileSystem>();
                FileSystem.GetFile(
                    Arg.Is<FilePath>(p => p.FullPath.EndsWith("MSBuild.exe", System.StringComparison.Ordinal)))
                    .Returns(c =>
                    {
                        // All requested files exist.
                        var file = Substitute.For<IFile>();
                        file.Exists.Returns(msBuildFileExist);
                        file.Path.Returns(c.Arg<FilePath>());
                        return file;
                    });                
            }
        }

        public void Run()
        {
            var runner = new MSBuildRunner(FileSystem, Environment, ProcessRunner, Globber);
            runner.Run(Solution, Settings);
        }

        public void AssertReceivedFilePath(FilePath path)
        {
            ProcessRunner.Received(1).Start(
                Arg.Is<FilePath>(p => p.FullPath == path.FullPath),
                Arg.Any<ProcessSettings>());   
        }

        public void AssertReceivedArguments(string format, params object[] args)
        {
            var arguments = string.Format(format, args);
            ProcessRunner.Received(1).Start(
                Arg.Any<FilePath>(),
                Arg.Is<ProcessSettings>(p =>
                    p.Arguments.Render() == arguments));   
        }
    }
}
