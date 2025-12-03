# Report Structure 
## Abstract (Jonathan & Jeremy)
- Brief summary of the project, objectives, and key findings

## 1. Introduction & Motivation (Jeremy)
- Context: Industry 4.0 pizza production system
- Problem statement and objectives
- Report organization

## 2. Problem & Approach (Jonathan)
- Problem description
- Research questions
- EAST-ADL development methodology (Artem will help)

## 3. Related Work (Jonathan & Sagor)
- Industry 4.0 manufacturing systems
- Microservices and message queues in production
- Software architecture design approaches

## 4. Requirements & Domain Analysis (Jonathan)

### 4.1 Use Cases
- UC04: Prepare Pizza
- UC11: Handle Restocking

### 4.2 Quality Attribute Scenarios
- Performance (NFR2: <30s pizza production)
- Availability (NFR1: 99% uptime)
- Integrability (NFR11: modular design)
- Key functional requirements overview

## 5. Architecture Development (Miguel)

### 5.1 Feature Model
- Feature model diagram
- Feature hierarchy and relationships
- Mandatory vs. optional features

### 5.2 Analysis Level Architecture
- System decomposition diagram
- Subsystems: Production Line, Warehouse, Web Interface
- Functional components and interfaces

### 5.3 Design Level Architecture
- Design level diagram
- Microservices structure (Controllers, Services, Dispatch)
- Technology decisions: C#, Python, Go, Kafka, databases
- Architectural patterns: Microservices, event-driven

### 5.4 Traceability Matrix
- Requirements to Feature Model to Architecture mapping

## 6. Behavioral Modeling & Verification (Artem)

### 6.1 State Machines (Miguel Help)
- Production line state machines
- Key process states and transitions

### 6.2 UPPAAL Timed Automata
- Model structure and templates
- Timing constraints specification

### 6.3 Formal Verification Results
- Safety properties (deadlock freedom)
- Timing properties verification
- Quality attribute validation

## 7. Experiment

### 7.1 Experiment Design (Artem)
- Objective: Validate performance (NFR2)
- Variables: Order latency, pizza production latency
- Setup and measurement methodology

### 7.2 Experiment Implementation (Miguel)
- Technology stack and polyglot architecture
- Deployment configuration
- Methodology
- Problems encountered and solutions


### 7.3 Results & Analysis (Artem & Miguel)
- Latency measurements
- Performance validation against requirements
- Bottleneck identification -> Dicuss solutions (horizontal/vertical scaling)

## 8. Evaluation & Discussion (Jeremy)
- Architecture effectiveness
- Verification and validation assessment
- Lessons learned

## 9. Conclusion (Jeremy)
- Key achievements
- Future work 
    - Scalability improvements (Bottle necks)
    - Test other quality attributes
    - Implement logging and monitoring

## 10. Contributions (All)
- Team member roles table

## References
- Cited literature and resources
- Uppaal documentation
- GitHub docs

## Appendices
- Feature model diagram
- State machine diagrams