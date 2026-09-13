Como levantar el proyecto:

1. En fedora crea la carpeta del proyecto y entra en ella
    mkdir tu-proyecto && cd tu-proyecto

2. Copiar el repo 
  git clone

3. Abrir proyecto en Visual Studio Code con el comando: 
  code .
4. Reabrir Visual Studio Code en el Dev Container
   Opcion 1: 
     Ctrl+Shift+P -> 'Dev Containers: Reopen in Container'. Esto construye Containerfile.dev, levanta tournament_db, instala extensiones de C# y corre dotnet restore automáticamente.
   Opcion 2 (más facil):
     Por lo general te aparece una ventana que te dice "Reopen in container" (o algo similar), y le das click. Para ello debes tener la extension de Dev containers.


5. Ya para cuando se quieran ejecutar los endpoints y probarlos, es necesario en la terminal integrada de VS Code (ya dentro del contenedor): dotnet watch run --project Tournament.Api . Cada vez que guardes un archivo .cs se recompila solo. (ESTO NO ES NECESARIO DE TODAVIA)

