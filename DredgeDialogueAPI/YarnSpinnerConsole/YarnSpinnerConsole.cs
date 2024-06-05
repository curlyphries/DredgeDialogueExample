namespace YarnSpinnerConsole
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Winch.Core;
    using Yarn;
    using Yarn.Compiler;

    /// <summary>
    /// Provides the entry point to the ysc command.
    /// </summary>
    public class YarnSpinnerConsole
    {

        // Compiles a given Yarn story. Designed to be called by runners or the
        // generic compile command. Does no writing.
        public static CompilationResult CompileProgram(FileInfo[] inputs)
        {
            // Given the list of files that we've received, figure out which Yarn files to compile. (If we were given a Yarn Project, this method will figure out which source files to use.)
            inputs = CompileCommand.GetYarnFiles(inputs);

            var compilationJob = CompilationJob.CreateFromFiles(inputs.Select(fileInfo => fileInfo.FullName));

            // Declare the existence of 'visited' and 'visited_count'
            var visitedDecl = new DeclarationBuilder()
                .WithName("visited")
                .WithType(
                    new FunctionTypeBuilder()
                        .WithParameter(Yarn.BuiltinTypes.String)
                        .WithReturnType(Yarn.BuiltinTypes.Boolean)
                        .FunctionType)
                .Declaration;

            var visitedCountDecl = new DeclarationBuilder()
                .WithName("visited_count")
                .WithType(
                    new FunctionTypeBuilder()
                        .WithParameter(Yarn.BuiltinTypes.String)
                        .WithReturnType(Yarn.BuiltinTypes.Number)
                        .FunctionType)
                .Declaration;

            compilationJob.VariableDeclarations = (compilationJob.VariableDeclarations ?? Array.Empty<Declaration>()).Concat(new[] {
                visitedDecl,
                visitedCountDecl,
            });

            CompilationResult compilationResult;

            try
            {
                compilationResult = Compiler.Compile(compilationJob);
            }
            catch (Exception e)
            {
                var errorBuilder = new StringBuilder();

                errorBuilder.AppendLine("Failed to compile because of the following error:");
                errorBuilder.AppendLine(e.ToString());

                WinchCore.Log.Error(errorBuilder.ToString());
                Environment.Exit(1);

                // Environment.Exit will stop the program before here;
                // throw an exception so the compiler doesn't wonder why
                // we're not returning a value.
                throw new Exception();
            }

            return compilationResult;
        }
    }
}