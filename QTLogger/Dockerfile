#Specify base image for build and publish
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

RUN dotnet --version

#Copy project file
COPY *.csproj ./

#install all specified dependencies
RUN dotnet restore

#Copy and Build
COPY . ./
#copy our app files and build our app
RUN dotnet publish -c Release -o out

#Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
#copy our built files to app/out
COPY --from=build-env /app/out .
#Specify how to start the app, takes an array that transforms into a command-line invocation with arguments
ENTRYPOINT ["StonkAtlas.QTLogger.exe", ""]
