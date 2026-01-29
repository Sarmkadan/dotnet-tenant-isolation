.PHONY: help build test clean restore run docs docker-build docker-up docker-down format lint migrate

# Variables
DOTNET := dotnet
PROJECT := src/TenantIsolation/TenantIsolation.csproj
DOCKER_IMAGE := tenant-isolation-api
DOCKER_TAG := latest

# Default target
help:
	@echo "Makefile targets for dotnet-tenant-isolation"
	@echo ""
	@echo "Building:"
	@echo "  make build          Build the project in Release mode"
	@echo "  make build-debug    Build the project in Debug mode"
	@echo "  make rebuild        Clean and rebuild the project"
	@echo ""
	@echo "Testing:"
	@echo "  make test           Run unit tests"
	@echo "  make test-verbose   Run tests with verbose output"
	@echo "  make test-coverage  Run tests with code coverage"
	@echo ""
	@echo "Development:"
	@echo "  make clean          Remove build artifacts"
	@echo "  make restore        Restore NuGet packages"
	@echo "  make run            Run the application"
	@echo "  make format         Format code using dotnet-format"
	@echo "  make lint           Run code quality checks"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build   Build Docker image"
	@echo "  make docker-up      Start Docker containers (docker-compose)"
	@echo "  make docker-down    Stop Docker containers"
	@echo ""
	@echo "Database:"
	@echo "  make migrate        Run database migrations"
	@echo "  make migrate-add    Add a new migration"
	@echo ""
	@echo "Documentation:"
	@echo "  make docs           Generate/view documentation"
	@echo ""
	@echo "Utilities:"
	@echo "  make outdated       Check for outdated NuGet packages"
	@echo "  make deps           Show project dependencies"
	@echo ""

# Build targets
build:
	$(DOTNET) build $(PROJECT) -c Release

build-debug:
	$(DOTNET) build $(PROJECT) -c Debug

rebuild: clean build
	@echo "✓ Rebuild complete"

# Test targets
test:
	$(DOTNET) test --configuration Release --no-build

test-verbose:
	$(DOTNET) test --configuration Release --no-build --verbosity normal

test-coverage:
	$(DOTNET) test --configuration Release --collect:"XPlat Code Coverage" --no-build

# Cleaning
clean:
	$(DOTNET) clean $(PROJECT)
	rm -rf bin obj publish
	@echo "✓ Clean complete"

# Restore
restore:
	$(DOTNET) restore

# Development
run:
	$(DOTNET) run --project $(PROJECT)

format:
	$(DOTNET) format

lint:
	$(DOTNET) build $(PROJECT) --no-restore -c Release

# Docker targets
docker-build:
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) .
	@echo "✓ Docker image built: $(DOCKER_IMAGE):$(DOCKER_TAG)"

docker-up:
	docker-compose up -d
	@echo "✓ Docker containers started"
	@echo "  API: http://localhost:5000"
	@echo "  Database: localhost:1433"

docker-down:
	docker-compose down
	@echo "✓ Docker containers stopped"

docker-logs:
	docker-compose logs -f api

docker-clean:
	docker-compose down -v
	docker rmi $(DOCKER_IMAGE):$(DOCKER_TAG)
	@echo "✓ Docker cleanup complete"

# Database targets
migrate:
	$(DOTNET) ef database update --project $(PROJECT)
	@echo "✓ Database migrated"

migrate-add:
	@read -p "Migration name: " name; \
	$(DOTNET) ef migrations add $$name --project $(PROJECT)

migrate-remove:
	$(DOTNET) ef migrations remove --project $(PROJECT)
	@echo "✓ Last migration removed"

migrate-rollback:
	$(DOTNET) ef database update 0 --project $(PROJECT)
	@echo "⚠ Database rolled back to initial state"

# Documentation
docs:
	@echo "Opening documentation..."
	@if command -v open > /dev/null; then \
		open README.md; \
	elif command -v xdg-open > /dev/null; then \
		xdg-open README.md; \
	else \
		echo "README.md"; \
	fi

# Utilities
outdated:
	$(DOTNET) list package --outdated

deps:
	$(DOTNET) list package

install-tools:
	$(DOTNET) tool install -g dotnet-format
	$(DOTNET) tool install -g dotnet-ef
	@echo "✓ Development tools installed"

# Combined targets
all: clean restore build test
	@echo "✓ Full build complete"

dev-setup: restore
	$(DOTNET) ef database update --project $(PROJECT)
	@echo "✓ Development environment ready"

ci: clean restore build test lint
	@echo "✓ CI pipeline complete"

# Help for specific commands
build-help:
	@echo "Build Commands:"
	@echo "  build       - Build Release configuration"
	@echo "  build-debug - Build Debug configuration"
	@echo "  rebuild     - Clean and build"

test-help:
	@echo "Test Commands:"
	@echo "  test              - Run all tests"
	@echo "  test-verbose      - Run tests with verbose output"
	@echo "  test-coverage     - Run tests with code coverage report"

docker-help:
	@echo "Docker Commands:"
	@echo "  docker-build  - Build Docker image"
	@echo "  docker-up     - Start services with docker-compose"
	@echo "  docker-down   - Stop services"
	@echo "  docker-logs   - View API logs"
	@echo "  docker-clean  - Remove containers and images"

migrate-help:
	@echo "Database Migration Commands:"
	@echo "  migrate        - Apply pending migrations"
	@echo "  migrate-add    - Create new migration"
	@echo "  migrate-remove - Remove last migration"
	@echo "  migrate-rollback - Rollback to initial state"
