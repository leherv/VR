FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
LABEL stage=builder
WORKDIR /app

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

# install chromium and some dependencies as well as dumb-init (to avoid zombie-processes, see: https://github.com/Yelp/dumb-init and https://blog.phusion.nl/2015/01/20/docker-and-the-pid-1-zombie-reaping-problem/)
RUN apt-get update && apt-get -f install && apt-get -y install dumb-init wget gnupg2 apt-utils
RUN apt-get install -y chromium fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst \
      --no-install-recommends
# set path to chromium executable
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/chromium"

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["dotnet", "VR.dll"]