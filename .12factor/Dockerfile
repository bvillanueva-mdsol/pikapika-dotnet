# syntax = docker/dockerfile:experimental
FROM 767904627276.dkr.ecr.us-east-1.amazonaws.com/mdsol/dotnet50-sdk as app-build
LABEL maintainer="Team 51 <team51@mdsol.com>"

WORKDIR /home/app
COPY . .

# restore and publish dotnet projects
RUN --mount=type=secret,id=artifactory,target=/home/app/env.sh .12factor/add-nuget-source.sh

RUN dotnet publish ./src/Medidata.Pikapika.Worker --configuration Release --output /out/worker

FROM 767904627276.dkr.ecr.us-east-1.amazonaws.com/mdsol/dotnet50-runtime AS final

WORKDIR /home/app

# copy necessary binaries and files from app-build
COPY --from=app-build /out ./

# setup and run the app
RUN chown -R app:app /home/app
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Medidata.Pikapika.Worker.dll"]
