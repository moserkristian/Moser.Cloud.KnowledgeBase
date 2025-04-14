# 📖 Moser.Cloud.KnowledgeBase Documentation

This repository serves as a **comprehensive resource** for understanding and implementing **enterprise-level distributed architectures** using **Clean Architecture, Microservices, CQRS, Event-Driven Design, and modern cloud-native best practices**.

This documentation is divided into multiple sections, each focusing on different **key aspects** of the platform.

---

## 📜 **Table of Contents**
- [🏗️ Clean Architecture](#-clean-architecture)
  - ~~[Best Practices](CleanArchitecture/BestPractices.md)~~
  - ~~[Layers & Responsibilities](CleanArchitecture/Layers.md)~~
  - ~~[Domain-Driven Design (DDD)](CleanArchitecture/DDD.md)~~
  - ~~[CQRS (Command Query Responsibility Segregation)](CleanArchitecture/CQRS.md)~~
  - ~~[Event-Driven Architecture](CleanArchitecture/EventDriven.md)~~
  - [Domain Layer Structure](Architecture/CleanArchitecture/DomainLayer.md)
  - [Application Layer Structure](Architecture/CleanArchitecture/ApplicationLayer.md)
  - [Infrastructure Layer Structure](Architecture/CleanArchitecture/InfrastructureLayer.md)
  - [Api Layer Structure](Architecture/CleanArchitecture/ApiLayer.md)
  - ~~[Testing Strategies](CleanArchitecture/TestingStrategies.md)~~

- [🏛️ Architecture](#-architecture)
  - [Microservices Overview](Architecture/Microservices.md)~~
  - [Messaging](Architecture/Messaging.md)
  - ~~[API Gateway Design](Architecture/API-Gateway.md)~~
  - ~~[Database Strategy](Architecture/Database-Strategy.md)~~
  - ~~[Scalability & Replication](Architecture/Scalability.md)~~
  - ~~[Security & Authentication](Architecture/Security.md)~~

- [☁️ Cloud-Native](#-cloud-native)
  - ~~[Deployment Strategies](Cloud/Deployment.md)~~
  - ~~[CI/CD Pipelines](Cloud/DevOps.md)~~
  - ~~[Monitoring & Observability](Cloud/Monitoring.md)~~

- [⚙️ DevOps & Infrastructure](#-devops--infrastructure)
  - ~~[GitHub Actions (CI/CD)](DevOps/GitHub-Actions.md)~~
  - ~~[Infrastructure as Code (IaC)](DevOps/Infrastructure-as-Code.md)~~

---

/docs/                               # 📖 Documentation
	/Architecture/                   # 🏛️ High-level system design & microservices
		Microservices.md             # 📜 List of microservices & responsibilities
		Messaging.md                 # 📬 Event-driven communication (Azure Service Bus/Kafka)
		~~API-Gateway.md~~               # 🚪 Gateway architecture (YARP/Ocelot)
		~~Database-Strategy.md~~         # 💾 Best DB choices per microservice (EF Core, raw SQL)
		~~Scalability.md~~               # 🚀 Horizontal scaling, replication, multi-region setups
		~~Security.md~~                  # 🔒 Authentication & authorization strategy
	/CleanArchitecture/              # 🏗️ Clean Architecture principles & implementation
		~~BestPractices.md~~             # ✅ Core principles of Clean Architecture
		~~Layers.md~~                    # 📂 Explanation of API, Application, Domain, Infrastructure
		~~CQRS.md~~                      # 🔄 Command Query Responsibility Segregation
		~~DDD.md~~                       # 📌 Domain-Driven Design overview
		~~EventDriven.md~~               # 📬 Event-Driven Architecture (patterns & examples)
		~~TestingStrategies.md~~         # 🧪 Unit, integration, end-to-end testing approaches
	/Cloud/                          # ☁️ Cloud-native concepts & infrastructure
		~~Deployment.md~~                # 🚀 Best practices for cloud deployment (Azure, AWS, Kubernetes)
		~~DevOps.md~~                    # 🔄 CI/CD pipelines, Infrastructure as Code
		~~Monitoring.md~~                # 📊 Observability, logging, OpenTelemetry
	/DevOps/                         # ⚙️ CI/CD, Infrastructure, Automation
		~~GitHub-Actions.md~~            # 🔄 CI/CD Pipelines for testing & deployment
		~~Infrastructure-as-Code.md~~    # 🏗️ Terraform, Bicep, Pulumi setup
	README.md                        # 📜 High-level documentation overview