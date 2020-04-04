Cypress.Commands.add('convertAssert', () => {
    cy.get('#csharp').should('be.visible')
    cy.get('#errors').should('not.be.visible')
    cy.get('#warnings').should('not.be.visible')
})
